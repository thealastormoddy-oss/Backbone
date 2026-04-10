LabSyncBackbone - Current Developer Documentation
===============================================

Purpose
-------
LabSyncBackbone is an internal middleware API that sits between internal apps and external systems.

Right now, the current implemented flow is centered around:
- receiving a request from an internal caller
- validating it
- checking local storage first
- calling an external app only if needed
- storing successful results locally
- saving failed calls for retry
- running background workers for cleanup, reconciliation, and retries

At the moment, the only active example integration is "mock". The structure is designed so more integrations can be added later in the same pattern. :contentReference[oaicite:0]{index=0} :contentReference[oaicite:1]{index=1}


1. High-level architecture
--------------------------

Main parts:
- Controllers
- SyncService
- ExternalAppRegistry
- Request mappers
- External app clients
- Local storage layer
- Repositories
- Workers

Simple view:

Internal Caller
  -> Controller
  -> Validator
  -> SyncService
      -> CaseStore
          -> Redis
          -> Postgres
      -> ExternalAppRegistry
          -> RequestMapper
          -> ExternalAppClient
      -> FailedRequestRepository
      -> RetryTrigger

Background:
- CleanupWorker
- ReconciliationWorker
- RetryWorker

Important idea:
- SyncService is the main orchestration service.
- ExternalAppRegistry is a lookup table for app-specific pieces.
- RequestMapper transforms internal request shape into external request shape.
- ExternalAppClient sends the mapped request to the external system.
- CaseStore hides the Redis + Postgres lookup/save logic. :contentReference[oaicite:2]{index=2} :contentReference[oaicite:3]{index=3} :contentReference[oaicite:4]{index=4}


2. Main request models
----------------------

Internal incoming request:
- SyncRequest
  - LocalAppName
  - RecordId
  - Payload

Payload:
- SyncPayload
  - CustomerName
  - CustomerCode
  - Amount
  - Notes :contentReference[oaicite:5]{index=5} :contentReference[oaicite:6]{index=6}

External outgoing request:
- ExternalAppRequest
  - SystemName
  - SubmissionId
  - Customer
  - Order
  - Comments :contentReference[oaicite:7]{index=7}

Internal response returned by middleware:
- SyncResponse
  - Message
  - ReceivedFrom
  - RecordId
  - Payload
  - NextStep
  - ExternalStatus
  - ExternalReference
  - ExternalMessage
  - SentToExternalApp :contentReference[oaicite:8]{index=8}

External response expected from external app:
- ExternalAppResponse
  - Status
  - ExternalReference
  - Message :contentReference[oaicite:9]{index=9}


3. Current active endpoint surface
----------------------------------

Health endpoint:
- GET /health

Mock endpoints:
- POST /mock/send
- GET /mock/get/{caseId}
- GET /mock/caselist  :contentReference[oaicite:11]{index=11}


4. Current "mock" request flow
------------------------------

This is the main implemented example.

4.1 POST /mock/send
-------------------

The request enters MockController.

MockController does two things first:
- validates the incoming request using SyncRequestValidator
- calls SyncService.ProcessAsync(request, "mock", "mock/send") :contentReference[oaicite:12]{index=12} :contentReference[oaicite:13]{index=13}

Important detail:
- the controller hardcodes "mock"
- that means this controller always uses the mock integration path :contentReference[oaicite:14]{index=14}

4.2 Validation
--------------

SyncRequestValidator checks:
- request body exists
- LocalAppName exists
- RecordId exists
- Payload exists
- Payload.CustomerName exists
- Payload.CustomerCode exists :contentReference[oaicite:15]{index=15}

If validation fails:
- controller returns 400 BadRequest with a message :contentReference[oaicite:16]{index=16}

4.3 SyncService main flow
-------------------------

Inside ProcessAsync:

Step 1:
- read caseId from request.RecordId

Step 2:
- ask CaseStore.Find(caseId)

CaseStore.Find does:
- check Redis first
- if not in Redis, check Postgres
- if found in Postgres, reload Redis and return it
- if not found anywhere, return null :contentReference[oaicite:17]{index=17} :contentReference[oaicite:18]{index=18}

If local data is found:
- SyncService returns that cached response immediately
- Message is changed to "Response returned from cache." :contentReference[oaicite:19]{index=19}

If local data is not found:
- SyncService logs local miss
- gets the mapper from ExternalAppRegistry using appName
- gets the client from ExternalAppRegistry using appName
- maps the request
- sends it to the external app :contentReference[oaicite:20]{index=20} :contentReference[oaicite:21]{index=21}

For "mock":
- mapper = MockRequestMapper
- client = MockExternalAppClient :contentReference[oaicite:22]{index=22} :contentReference[oaicite:23]{index=23}

4.4 Mapper flow for "mock"
--------------------------

MockRequestMapper converts SyncRequest into ExternalAppRequest.

Mapping rules:
- LocalAppName -> SystemName
- RecordId -> SubmissionId
- Payload.CustomerName -> Customer.Name
- Payload.CustomerCode -> Customer.Code
- Payload.Amount -> Order.TotalAmount
- Payload.Notes -> Comments :contentReference[oaicite:24]{index=24} :contentReference[oaicite:25]{index=25}

Example:

Incoming SyncRequest:
{
  "localAppName": "LabA",
  "recordId": "CASE-123",
  "payload": {
    "customerName": "Alice",
    "customerCode": "A100",
    "amount": 250.75,
    "notes": "Urgent"
  }
}

Mapped ExternalAppRequest:
{
  "systemName": "LabA",
  "submissionId": "CASE-123",
  "customer": {
    "name": "Alice",
    "code": "A100"
  },
  "order": {
    "totalAmount": 250.75
  },
  "comments": "Urgent"
} :contentReference[oaicite:26]{index=26} :contentReference[oaicite:27]{index=27} :contentReference[oaicite:28]{index=28} :contentReference[oaicite:29]{index=29}

4.5 External app call for "mock"
--------------------------------

MockExternalAppClient:
- uses HttpClient
- posts the mapped request as JSON to configured ReceivePath
- handles success, non-success HTTP status, timeout, connection failure, and unexpected exceptions 

Possible outcomes:
- external success
- external HTTP failure
- no response body
- timeout
- network error
- unexpected exception 

4.6 Response building
---------------------

After external response returns, SyncService builds SyncResponse.

This response includes:
- original request info
- external result fields
- the outgoing external request shape that was sent

This is useful for debugging and visibility. :contentReference[oaicite:32]{index=32} :contentReference[oaicite:33]{index=33}

4.7 Success path
----------------

If external status is "Success":
- save to CaseStore
- trigger RetryTrigger

CaseStore.Save:
- saves to Postgres
- saves to Redis :contentReference[oaicite:34]{index=34} :contentReference[oaicite:35]{index=35}

Why trigger RetryTrigger after success?
- so RetryWorker can wake up early and retry pending failed requests instead of waiting for the full timer interval :contentReference[oaicite:36]{index=36} :contentReference[oaicite:37]{index=37}

4.8 Failure path
----------------

If external status is not "Success":
- save a FailedRequest row
- record attempt count, failure reason, next retry time, status, created time :contentReference[oaicite:38]{index=38} :contentReference[oaicite:39]{index=39}

The first failure is saved with:
- AttemptCount = 1
- Status = Pending
- NextRetryAt = now + exponential backoff base calculation used by SyncService :contentReference[oaicite:40]{index=40}

4.9 Controller response
-----------------------

After SyncService returns:
- if ExternalStatus is not "Success", controller returns HTTP 502
- if ExternalStatus is "Success", controller returns HTTP 200 :contentReference[oaicite:41]{index=41}


5. Read flow
------------

5.1 GET /mock/get/{caseId}
--------------------------

MockController calls SyncService.GetCase(caseId). :contentReference[oaicite:42]{index=42}

GetCase simply calls CaseStore.Find(caseId). :contentReference[oaicite:43]{index=43}

Lookup order:
- Redis first
- Postgres second
- if found in Postgres, Redis is reloaded
- if not found, returns null :contentReference[oaicite:44]{index=44}

If not found:
- controller returns 404

If found:
- controller returns 200 with SyncResponse :contentReference[oaicite:45]{index=45}

5.2 GET /mock/caselist
----------------------

MockController calls SyncService.GetAllCaseIds(). :contentReference[oaicite:46]{index=46}

GetAllCaseIds does:
- run reconciliation first
- then return union of known keys from storage :contentReference[oaicite:47]{index=47}

This means calling caselist does more than read a list.
It also attempts to make Redis and Postgres consistent first. :contentReference[oaicite:48]{index=48} 


6. Local storage design
-----------------------

There are two local storage layers:
- Redis for fast cache
- Postgres for persisted storage

CaseStore is the abstraction in front of them. :contentReference[oaicite:50]{index=50}

6.1 Redis
---------

RedisCacheService:
- stores JSON strings
- reads JSON strings
- deletes keys
- lists keys
- uses expiration seconds from CacheSettings :contentReference[oaicite:51]{index=51} :contentReference[oaicite:52]{index=52}

6.2 Postgres
------------

CaseRepository:
- saves SyncResponse as JSON
- stores metadata:
  - CaseId
  - AppName
  - Data
  - CachedAt
  - ExpiresAt
- lazy-deletes expired rows on Get
- supports DeleteExpired for cleanup worker :contentReference[oaicite:53]{index=53} :contentReference[oaicite:54]{index=54}

CachedCase is stored as jsonb in Postgres. :contentReference[oaicite:55]{index=55}

6.3 Why both Redis and Postgres?
--------------------------------

Reason:
- Redis is fast but temporary
- Postgres gives a more durable local copy
- CaseStore lets the app use both without controllers caring about the details :contentReference[oaicite:56]{index=56}


7. Background workers
---------------------

Three hosted workers are registered:
- CleanupWorker
- ReconciliationWorker
- RetryWorker :contentReference[oaicite:57]{index=57}

7.1 CleanupWorker
-----------------

Purpose:
- delete expired cached cases from Postgres on an interval

Flow:
- wait configured interval
- create a scope
- resolve ICaseRepository
- call DeleteExpired
- log deleted count  :contentReference[oaicite:59]{index=59}

Important:
- this worker uses IServiceScopeFactory because it needs scoped services inside a singleton hosted worker 

7.2 ReconciliationWorker
------------------------

Purpose:
- keep Redis and Postgres aligned

Flow:
- wait configured interval
- create a scope
- resolve IReconciliationService
- call Reconcile
- log results 

Reconciliation rules:
- if case exists in Postgres but not Redis, reload it into Redis
- if case exists in Redis but not Postgres, save it into Postgres using appName "unknown" 

7.3 RetryWorker
---------------

Purpose:
- retry failed external requests

Wake-up behavior:
- wakes by timer
- or wakes early if RetryTrigger is signaled after a success  :contentReference[oaicite:64]{index=64}

Flow:
- get pending retries due now
- deserialize stored request payload
- call SyncService.ProcessAsync again
- mark succeeded if retry works
- otherwise increment attempt count and apply exponential backoff
- mark exhausted when max attempts reached  :contentReference[oaicite:66]{index=66}

FailedRequest statuses:
- Pending
- Succeeded
- Exhausted :contentReference[oaicite:67]{index=67}


8. Dependency injection setup
-----------------------------

Current important registrations include:
- ICacheService -> RedisCacheService (singleton)
- ICaseStore -> CaseStore (scoped)
- SyncService (scoped)
- MockExternalAppClient via AddHttpClient
- ExternalAppRegistry (singleton)
- ICaseRepository -> CaseRepository (scoped)
- IReconciliationService -> ReconciliationService (scoped)
- IFailedRequestRepository -> FailedRequestRepository (scoped)
- MockRequestMapper (singleton)
- all workers as hosted services :contentReference[oaicite:68]{index=68}

Important design idea:
- one registry is shared for the whole app
- app-specific mapper/client selection is done at runtime by appName
- current controller passes "mock", so registry returns mock pieces :contentReference[oaicite:69]{index=69} :contentReference[oaicite:70]{index=70} :contentReference[oaicite:71]{index=71}

Example:
- registry stores:
  - "mock" -> MockExternalAppClient
  - "mock" -> MockRequestMapper
- SyncService.ProcessAsync(request, "mock", ...)
- registry returns the objects registered under "mock" :contentReference[oaicite:72]{index=72} :contentReference[oaicite:73]{index=73}


9. Edge cases and current behavior
----------------------------------

9.1 Cache hit
-------------

If a case is already in Redis or Postgres:
- no external call happens
- a cached response is returned immediately
- response message is changed to "Response returned from cache." :contentReference[oaicite:74]{index=74}

Result:
- faster response
- less external traffic

9.2 Redis miss, Postgres hit
----------------------------

Behavior:
- return Postgres result
- reload Redis automatically :contentReference[oaicite:75]{index=75}

This helps Redis recover after restart or eviction.

9.3 Expired Postgres entry
--------------------------

CaseRepository.Get checks ExpiresAt.
If expired:
- delete row
- treat as miss :contentReference[oaicite:76]{index=76}

This is lazy cleanup on read.

9.4 External app returns non-200 HTTP status
--------------------------------------------

MockExternalAppClient turns that into a failed ExternalAppResponse. 

Then SyncService:
- treats it as external failure
- stores FailedRequest for retry :contentReference[oaicite:78]{index=78}

9.5 External app returns empty body
-----------------------------------

MockExternalAppClient returns failed response with message "External app returned empty response." 

9.6 External app times out or is unreachable
--------------------------------------------

MockExternalAppClient catches timeout/network exceptions and returns failed response. 

Then SyncService stores the request for retry. :contentReference[oaicite:81]{index=81}

9.7 Retry payload cannot be deserialized
----------------------------------------

RetryWorker marks that failed request as Exhausted immediately. 

Reason:
- if the original request payload cannot be rebuilt, retry cannot safely continue

9.8 Reconciliation saves Redis-only data to Postgres with appName "unknown"
---------------------------------------------------------------------------

This is current behavior. 

This avoids losing data, but it means:
- original source appName may be lost in this case
- later analysis may be less precise

9.9 Caselist triggers reconciliation
------------------------------------

This is important because caselist is not just a simple read. :contentReference[oaicite:84]{index=84}

Possible effect:
- listing cases may do extra storage work first

That is fine for now, but it is worth knowing.

9.10 Failed request rows are not deleted after success
------------------------------------------------------

RetryWorker marks rows as Succeeded instead of deleting them. 

This is useful for history and audit visibility.


10. Design decisions and tradeoffs
----------------------------------

10.1 One orchestration service
------------------------------

Current choice:
- SyncService is the main orchestrator

Why it makes sense:
- the overall flow is still one workflow
- only the app-specific mapping and external call vary

Tradeoff:
- if many integrations later need very different business flows, SyncService may become too large and need splitting :contentReference[oaicite:86]{index=86}

10.2 Registry-based selection instead of direct multi-injection
---------------------------------------------------------------

Current choice:
- inject one ExternalAppRegistry
- select client/mapper by appName at runtime

Why it makes sense:
- avoids ambiguity if many mappers/clients exist
- keeps SyncService generic

Tradeoff:
- appName strings become important
- wrong key means runtime failure, not compile-time failure :contentReference[oaicite:87]{index=87}

10.3 Redis + Postgres together
------------------------------

Current choice:
- use Redis for speed
- use Postgres for persisted local copy

Why it makes sense:
- balances performance and durability

Tradeoff:
- now there are two stores to keep consistent
- requires reconciliation and cleanup logic :contentReference[oaicite:88]{index=88} 

10.4 Workers use IServiceScopeFactory
-------------------------------------

Current choice:
- workers are hosted services
- they create scopes when they need scoped dependencies

Why it makes sense:
- repositories and db context are scoped
- hosted services are singleton-style background services

Tradeoff:
- a little more ceremony
- but correct lifetime handling   

10.5 Retry rows kept as history
-------------------------------

Current choice:
- failed request rows are updated to Succeeded or Exhausted

Why it makes sense:
- allows review of retry history

Tradeoff:
- table will grow over time and may later need archival/cleanup policy 

10.6 Controller hardcodes integration key
-----------------------------------------

Current choice:
- MockController passes "mock" explicitly to SyncService :contentReference[oaicite:94]{index=94}

Why it makes sense:
- very clear for current stage
- simple and easy to reason about

Tradeoff:
- more controllers may mean repeated wiring logic
- a more generic route-based approach may be needed later


11. Current known limitations
-----------------------------

- Only the "mock" integration is active right now. :contentReference[oaicite:95]{index=95}
- ExternalAppRegistry uses string keys, so typos are runtime issues. :contentReference[oaicite:96]{index=96}
- SyncService is still concrete, not behind an interface yet. :contentReference[oaicite:97]{index=97}
- MockController depends on SyncService directly, not an abstraction. :contentReference[oaicite:98]{index=98}
- caselist performs reconciliation before listing, which may be heavier than expected. :contentReference[oaicite:99]{index=99}
- reconciliation can save missing Postgres rows with appName "unknown". 
- retry history can grow over time without cleanup. 
- CaseRepository unique index is on CaseId only, not CaseId + AppName. That means the same CaseId cannot exist separately per app in current design. :contentReference[oaicite:102]{index=102} :contentReference[oaicite:103]{index=103}


12. "mock" integration summary
------------------------------

For the current mock integration:

Request path:
- POST /mock/send

Flow:
- validate request
- check local storage
- if found locally, return cached response
- if not found, map request to external mock shape
- send to mock external app
- if success, save to Redis and Postgres
- if failure, store for retry
- return SyncResponse to caller :contentReference[oaicite:104]{index=104} :contentReference[oaicite:105]{index=105} :contentReference[oaicite:106]{index=106} 

This is the clearest example for understanding the current system.


13. Startup behavior
--------------------

On startup:
- services are registered
- database context is configured
- pending migrations are automatically applied inside a startup scope
- controllers and swagger/openapi are enabled
- hosted workers start running after app startup :contentReference[oaicite:108]{index=108}

This means the app attempts to keep the database schema up to date automatically.


14. Short future direction
--------------------------

Only brief mention, since this document is about current state:

The current structure is already prepared for future integrations by following the same pattern:
- register another external client
- register another request mapper
- add them into ExternalAppRegistry under a new key such as "cpm"
- add a controller or another way to choose that integration key at runtime :contentReference[oaicite:109]{index=109} :contentReference[oaicite:110]{index=110}

So the current app is small, but the extension path is already visible.


15. Quick glossary
------------------

SyncService:
- the main workflow/orchestration service

ExternalAppRegistry:
- lookup table for integration-specific clients and mappers

RequestMapper:
- converts internal request shape into external request shape

ExternalAppClient:
- sends mapped request to external system

CaseStore:
- unified access point over Redis and Postgres

CleanupWorker:
- removes expired Postgres cache rows

ReconciliationWorker:
- repairs Redis/Postgres differences

RetryWorker:
- retries failed external requests


End of current-state documentation.
