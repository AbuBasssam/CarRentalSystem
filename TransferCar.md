# 🧪 API Test Cases — Transfer Car to Another Branch

> **Project:** Car Rental API — C# / ASP.NET Core
> **Testing Tool:** Postman (Manual)
> **Total Cases:** 11 test cases
> **Endpoint:** `POST {{baseUrl}}/api/v1/cars/{{carId}}/transfer`

---

## 📖 Legend

| Symbol | Meaning                                                                    |
|---------|---------------------------------------------------------------------------|
| ✅      | Happy Path — expected successful flow                                    |
| ❌      | Error Case — expected failure                                            |
| ⚠️      | Edge Case — boundary or rare condition                                   |
| 🔐      | Auth Case — authentication (TODO: enable after activating `[Authorize]`) |
|`TC-XX`  | Test case number                                                         |

---

## 🗺️ What This Endpoint Does

Transfers a car from its **current branch** to a **target branch**, and logs a `CarBranchHistory` record for audit purposes.

**Business Rules (from Handler + Car entity):**
- Car must exist → otherwise `404`
- Target branch must exist **and** be active → otherwise `400`
- Target branch must be **different** from the car's current branch → otherwise `400`
- A `CarBranchHistory` record is always created on a successful transfer

---

## Summary

| # | Test Case | Type | Expected Status Code |
|---|-----------|------|----------------------|
| TC-01 | Transfer car to a valid active branch | ✅ Happy Path | `200 OK` |
| TC-02 | Transfer with an optional Reason provided | ✅ Happy Path | `200 OK` |
| TC-03 | Car does not exist | ❌ Error Case | `404 Not Found` |
| TC-04 | Target branch does not exist | ❌ Error Case | `400 Bad Request` |
| TC-05 | Target branch exists but is inactive | ❌ Error Case | `400 Bad Request` |
| TC-06 | Target branch is the same as current branch | ❌ Error Case | `400 Bad Request` |
| TC-07 | ToBranchId = 0 (invalid value) | ❌ Error Case | `422 Unprocessable Entity` |
| TC-08 | Empty request body | ❌ Error Case | `422 Unprocessable Entity` |
| TC-09 | Reason exceeds 250 characters | ⚠️ Edge Case | `422 Unprocessable Entity` |
| TC-10 | Reason is exactly 250 characters (boundary) | ⚠️ Edge Case | `200 OK` |
| TC-11 | Transfer with valid Cookie but Customer role | 🔐 Auth Case | `403 Forbidden` |

---

## Test Case Details

---

### ✅ TC-01 — Transfer car to a valid active branch

**Pre-conditions:**
- Car with `Id=15` exists and `CurrentBranchId=1`
- Branch with `Id=2` exists and `IsActive=true`

**Postman Setup:**
```
Method  : POST
URL     : {{baseUrl}}/api/v1/cars/15/transfer
Headers : Content-Type: application/json
```

**Request Body:**
```json
{
  "toBranchId": 2
}
```

**Expected Response — `200 OK`:**
```json
{
  "statusCode": 200,
  "succeeded": true,
  "message": "Success",
  "data": true
}
```

> 💡 `reason` is nullable — omitting it is valid. A `CarBranchHistory` record is created in the database logging `FromBranchId=1`, `ToBranchId=2`, and `MovedAt=UtcNow`.

---

### ✅ TC-02 — Transfer with an optional Reason provided

**Pre-conditions:** Same as TC-01 with a different target branch.

**Request Body:**
```json
{
  "toBranchId": 3,
  "reason": "Routine redistribution of fleet across branches."
}
```

**Expected Response — `200 OK`:**
```json
{
  "statusCode": 200,
  "succeeded": true,
  "message": "Success",
  "data": true
}
```

> 💡 When `reason` is provided it is stored in the `CarBranchHistory` record for audit purposes.

---

### ❌ TC-03 — Car does not exist

**Pre-conditions:** No car with `Id=9999` exists in the database.

**Postman Setup:**
```
Method  : POST
URL     : {{baseUrl}}/api/v1/cars/9999/transfer
Headers : Content-Type: application/json
```

**Request Body:**
```json
{
  "toBranchId": 2
}
```

**Expected Response — `404 Not Found`:**
```json
{
  "statusCode": 404,
  "succeeded": false,
  "message": "Car not found.",
  "errors": ["Car not found."]
}
```

---

### ❌ TC-04 — Target branch does not exist

**Pre-conditions:**
- Car with `Id=15` exists
- No branch with `Id=9999` exists in the database

**Request Body:**
```json
{
  "toBranchId": 9999
}
```

**Expected Response — `400 Bad Request`:**
```json
{
  "statusCode": 400,
  "succeeded": false,
  "message": "Target branch not found or is inactive.",
  "errors": ["Target branch not found or is inactive."]
}
```

> 💡 This is a **Business Validation** check inside the Handler — the branch query filters by `IsActive=true`, so both "not found" and "inactive" produce the same error message.

---

### ❌ TC-05 — Target branch exists but is inactive

**Pre-conditions:**
- Car with `Id=15` exists
- Branch with `Id=4` exists but `IsActive=false`

**Request Body:**
```json
{
  "toBranchId": 4
}
```

**Expected Response — `400 Bad Request`:**
```json
{
  "statusCode": 400,
  "succeeded": false,
  "message": "Target branch not found or is inactive.",
  "errors": ["Target branch not found or is inactive."]
}
```

> 💡 Same error message as TC-04 — the Handler uses `.AnyAsync(b => b.Id == request.Dto.ToBranchId && b.IsActive)` so both cases are indistinguishable from the API response perspective.

---

### ❌ TC-06 — Target branch is the same as current branch

**Pre-conditions:** Car with `Id=15` exists and `CurrentBranchId=1`.

**Request Body:**
```json
{
  "toBranchId": 1
}
```

**Expected Response — `400 Bad Request`:**
```json
{
  "statusCode": 400,
  "succeeded": false,
  "message": "Car is already at the target branch.",
  "errors": ["Car is already at the target branch."]
}
```

> 💡 The Handler checks `car.CurrentBranchId == request.Dto.ToBranchId` **after** confirming the branch exists and is active — so the branch validity is verified first.

---

### ❌ TC-07 — ToBranchId = 0 (invalid value)

**Description:** `ToBranchId` must be greater than `0`. Sending `0` or a negative number is rejected by FluentValidation before reaching the Handler.

**Request Body:**
```json
{
  "toBranchId": 0
}
```

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "validationErrors": {
    "Dto.ToBranchId": "ToBranchId is required."
  }
}
```

> 💡 Caught by `.GreaterThan(0)` in `TransferCarDto.Validator` — the message "is required" is the custom wording chosen in the validator for this rule.

---

### ❌ TC-08 — Empty request body

**Description:** Sending a POST with no body at all.

**Request Body:** *(empty — send nothing)*

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "message": "Request payload is required.",
  "errors": ["Request payload is required."]
}
```

> 💡 Caught by `.NotNull()` on the Dto in `TransferCarCommandValidator` before the inner Validator runs.

---

### ⚠️ TC-09 — Reason exceeds 250 characters

**Description:** The `Reason` field has a `MaximumLength(250)` rule. Any value longer than 250 characters must be rejected.

**Request Body:**
```json
{
  "toBranchId": 2,
  "reason": "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
}
```

*(The `reason` value above is 251 characters — one over the limit)*

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "validationErrors": {
    "Dto.Reason": "Reason cannot exceed 500 characters."
  }
}
```

> ⚠️ **Known Bug in `TransferCarDto.Validator`:** The rule enforces `MaximumLength(250)` but the error message says `"Reason cannot exceed 500 characters."` — there is a mismatch between the actual limit (250) and the message (500). The API will reject at 251 characters despite what the message says. This should be corrected in the code.

---

### ⚠️ TC-10 — Reason is exactly 250 characters (boundary)

**Description:** A `reason` of exactly 250 characters sits right at the boundary — it must be **accepted**.

**Request Body:**
```json
{
  "toBranchId": 2,
  "reason": "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
}
```

*(The `reason` value above is exactly 250 characters)*

**Expected Response — `200 OK`:**
```json
{
  "statusCode": 200,
  "succeeded": true,
  "message": "Success",
  "data": true
}
```

> 💡 `MaximumLength(250)` is inclusive — exactly 250 characters passes validation. The rule only applies `.When(x => !string.IsNullOrEmpty(x.Reason))`, so an empty string or `null` bypasses it entirely.

---

### 🔐 TC-11 — Transfer with valid Cookie but Customer role

> **TODO:** Enable after removing the comment from `[Authorize(Roles = Roles.Admin)]`.

**Description:** Cookie is valid but the user's role is `Customer` not `Admin`.

**How to test it:**
1. Sign in with a regular Customer account → `POST /api/v1/authentication/signin`
2. Send the transfer request — Postman will send the Cookie automatically

**Request Body:**
```json
{
  "toBranchId": 2
}
```

**Expected Response — `403 Forbidden`:**
```json
{
  "statusCode": 403,
  "succeeded": false,
  "message": "Forbidden",
  "errors": ["Forbidden"]
}
```

---

---

## 📋 Quick Reference

### Business Rule Execution Order (inside the Handler)

```
1. Does the car exist?              → No  : 404 Not Found
                                    → Yes : continue
2. Does the target branch exist
   and is it active?                → No  : 400 Bad Request
                                    → Yes : continue
3. Is the target branch different
   from the current branch?         → No  : 400 Bad Request
                                    → Yes : continue
4. Create CarBranchHistory record
5. Transfer car (update CurrentBranchId)
6. SaveChanges → return 200 OK
```

### TransferCarDto Field Constraints

| Field | Required | Constraint | Note |
|-------|----------|------------|------|
| `toBranchId` | ✅ Yes | `> 0` | Must be a valid active branch Id |
| `reason` | ❌ No | Max `250` characters | Error message in code incorrectly states 500 — see TC-09 |