using LabSyncBackbone.Helpers;
using LabSyncBackbone.Models;
using LabSyncBackbone.Services;
using Microsoft.AspNetCore.Mvc;

namespace LabSyncBackbone.Controllers
{
    [ApiController]
    [Route("mock")]
    public class MockController : ControllerBase
    {
        private readonly SyncService _syncService;
        private readonly SyncRequestValidator _validator;

        public MockController(SyncService syncService, SyncRequestValidator validator)
        {
            _syncService = syncService;
            _validator = validator;
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] SyncRequest request)
        {
            var validationError = _validator.Validate(request);

            if (validationError != null)
            {
                return BadRequest(new { Message = validationError });
            }

            var response = await _syncService.ProcessAsync(request, "mock", "mock/send");

            if (response.ExternalStatus != "Success")
            {
                return StatusCode(502, response);
            }

            return Ok(response);
        }

        [HttpGet("get/{caseId}")]
        public IActionResult GetCase(string caseId, [FromQuery] string? fields)
        {
            var response = _syncService.GetCase(caseId);

            if (response == null)
            {
                return NotFound(new { Message = "Case not found: " + caseId });
            }

            // If ?fields= was provided, return only those fields
            var filtered = FieldFilter.Apply(response, fields);
            if (filtered != null)
                return Ok(filtered);

            // Otherwise return the full object as before
            return Ok(response);
        }

        [HttpGet("caselist")]
        public IActionResult GetCaseList()
        {
            var caseIds = _syncService.GetAllCaseIds();

            return Ok(new { Count = caseIds.Count, CaseIds = caseIds });
        }
    }
}