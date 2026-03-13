# 🧪 API Test Cases — Update Car Status

> **Project:** Car Rental API — C# / ASP.NET Core
> **Testing Tool:** Postman (Manual)
> **Total Cases:** 11 test cases
> **Endpoint:** `PATCH {{baseUrl}}/api/v1/cars/{{carId}}/status`

---

## 📖 Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Happy Path — expected successful flow |
| ❌ | Error Case — expected failure |
| ⚠️ | Edge Case — boundary or rare condition |
| 🔐 | Auth Case — authentication (TODO: enable after activating `[Authorize]`) |
| `TC-XX` | Test case number |

---

---

---

# Section 3: Update Car Status

**Endpoint:** `PATCH {{baseUrl}}/api/v1/cars/{{carId}}/status`
**Content-Type:** `application/json`

> Both `isActive` and `fleetConditionStatus` are **nullable**.
> At least one of them must be provided — sending both is also valid.

---

## Summary

|   #   | Test Case                                      | Type            | Expected Status Code       |
|-------|------------------------------------------------|-----------------|----------------------------|
| TC-01 | Update IsActive only (deactivate)              | ✅ Happy Path  | `200 OK`                    |
| TC-02 | Update FleetConditionStatus only               | ✅ Happy Path  | `200 OK`                    |
| TC-03 | Update both fields in one request              | ✅ Happy Path  | `200 OK`                    |
| TC-04 | Update IsActive only (activate)                | ✅ Happy Path  | `200 OK`                    |
| TC-05 | Car does not exist                             | ❌ Error Case  | `404 Not Found`             |
| TC-06 | Both fields are null (empty object)            | ❌ Error Case  | `422 Unprocessable Entity`  |
| TC-07 | Completely empty request body                  | ❌ Error Case  | `422 Unprocessable Entity`  |
| TC-08 | Invalid FleetConditionStatus value             | ❌ Error Case  | `422 Unprocessable Entity`  |
| TC-08 | FleetConditionStatus already set to same value | ⚠️ Edge Case   | `200 OK`                    |
| TC-09 | IsActive already set to same value             | ⚠️ Edge Case   | `200 OK`                    |
| TC-10 | Create car with valid Cookie but Customer role | 🔐 Auth Case   | `403 Forbidden`             |

---

## FleetConditionStatus Reference

| Value | Name               | Description                           |
|-------|--------------------|---------------------------------------|
| `1`   | `Ready`            | Car is available and ready for rental |
| `2`   | `Rented`           | Car is currently on an active rental  |
| `3`   | `UnderMaintenance` | Car is in maintenance                 |
| `4`   | `OutOfService`     | Car is out of service                 |

> 💡 These values are based on `enFleetConditionStatus` enum. Confirm the exact integer mappings from the enum definition in your project.

---

## Test Case Details

---

### ✅ TC-01 — Update IsActive only (deactivate a car)

**Pre-conditions:** Car with `Id=15` exists and `IsActive=true`.

**Postman Setup:**
```
Method : PATCH
URL    : {{baseUrl}}/api/v1/cars/15/status
Headers: Content-Type: application/json
```

**Request Body:**
```json
{
  "isActive": false
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

> 💡 `fleetConditionStatus` is omitted entirely — that is valid since `IsActive` is provided. The Handler calls `car.Deactivate()` on the domain entity.

---

### ✅ TC-02 — Update FleetConditionStatus only

**Pre-conditions:** Car with `Id=15` exists.

**Request Body:**
```json
{
  "fleetConditionStatus": 2
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

> 💡 `isActive` is omitted entirely — valid since `fleetConditionStatus` is provided. The Handler calls `car.UpdateConditionStatus()`.

---

### ✅ TC-03 — Update both fields in one request

**Pre-conditions:** Car with `Id=15` exists.

**Request Body:**
```json
{
  "isActive": false,
  "fleetConditionStatus":3
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

> 💡 Both fields are updated in a single request. The Handler processes `IsActive` first, then `FleetConditionStatus`.

---

### ✅ TC-04 — Update IsActive only (activate a car)

**Pre-conditions:** Car with `Id=15` exists and `IsActive=false`.

**Request Body:**
```json
{
  "isActive": true
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

> 💡 The Handler calls `car.Activate()`. This is the reverse of TC-32 and worth testing independently.

---

### ❌ TC-04 — Car does not exist

**Pre-conditions:** No car with `Id=9999` exists in the database.

**Postman Setup:**
```
Method : PATCH
URL    : {{baseUrl}}/api/v1/cars/9999/status
Headers: Content-Type: application/json
```

**Request Body:**
```json
{
  "isActive": false
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

### ❌ TC-05 — Both fields are null (empty object)

**Description:** Sending a body where both fields are explicitly `null`. The Validator rule requires at least one field to have a value.

**Request Body:**
```json
{
  "isActive": null,
  "fleetConditionStatus": null
}
```

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "validationErrors": {
    "Dto": "At least one of IsActive or FleetConditionStatus must be provided."
  }
}
```

> 💡 Caught by `.Must(x => x.IsActive.HasValue || x.FleetConditionStatus.HasValue)` in `UpdateCarStatusDto.Validator`.

---

### ❌ TC-06 — Completely empty request body

**Description:** Sending a PATCH with no body at all.

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

> 💡 Caught by `.NotNull()` on the Dto in `UpdateCarStatusCommandValidator` before the inner Validator runs.

---

### ❌ TC-07 — Invalid FleetConditionStatus value

**Description:** Sending an integer that does not map to any value in `enFleetConditionStatus`.

**Request Body:**
```json
{
  "fleetConditionStatus": 99
}
```

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "validationErrors": {
    "Dto.FleetConditionStatus": "FleetConditionStatus must be a valid enum value."
  }
}
```

> 💡 Caught by `.IsInEnum().When(x => x.FleetConditionStatus.HasValue)` in the Validator.

---

### ⚠️ TC-08 — FleetConditionStatus already set to the same value

**Description:** Sending the same `FleetConditionStatus` the car already has. The API should still return success — it is an idempotent operation.

**Pre-conditions:** Car with `Id=15` has `FleetConditionStatus=0` (Ready) already.

**Request Body:**
```json
{
  "fleetConditionStatus": 0
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

> 💡 `UpdateConditionStatus()` in the `Car` entity checks `if (FleetConditionStatus == newStatus) return;` — it silently skips the update if the value is unchanged. The Handler then calls `SaveChangesAsync()` with no actual diff, and returns `Success(true)`. The `200 OK` is **confirmed**.

---

### ⚠️ TC-09 — IsActive already set to the same value

**Description:** Sending `isActive=true` for a car that is already active. Should still succeed.

**Pre-conditions:** Car with `Id=15` has `IsActive=true` already.

**Request Body:**
```json
{
  "isActive": true
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

> 💡 `Activate()` checks `if (IsActive) return;` and `Deactivate()` checks `if (!IsActive) return;` — both silently skip when the value is already set. No exception is thrown, `SaveChangesAsync()` runs with no diff, and the response is `Success(true)`. The `200 OK` is **confirmed**.

---

### 🔐 TC-10 — Update status with valid Cookie but Customer role

> **TODO:** Enable after removing the comment from `[Authorize(Roles = Roles.Admin)]`.

**Description:** Cookie is valid but the user's role is `Customer` not `Admin`.

**How to test it:**
1. Sign in with a regular Customer account → `POST /api/v1/authentication/signin`
2. Send the PATCH request — Postman will send the Cookie automatically

**Request Body:**
```json
{
  "isActive": false
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

## 📋 Quick Reference — Allowed Values

### FuelType

| Value | Name     |
|-------|----------|
| `1`   | Petrol   |
| `2`   | Diesel   |
| `3`   | Electric |
| `4`   | Hybrid   |

### TransmissionType

| Value | Name      |
|-------|-----------|
| `1`   | Manual    |
| `2`   | Automatic |

### Field Constraints

| Field              | Min                     | Max                     |
|--------------------|-------------------------|-------------------------|
| `year`             | `1990`                  | `current year + 1`      |
| `numberOfSeats`    | `1`                     | `15`                    |
| `numberOfBags`     | `0`                     | `10`                    |
| `engineCapacity`   | `0`                     | —                       |
| `plateNumberAR`    | —                       | `20` characters         |
| `brand`            | —                       | `50` characters         |
| `model`            | —                       | `50` characters         |
| `vin`              | exactly `17` characters | exactly `17` characters |
| Image file size    | —                       | `5 MB`                  |
| Images per request | `1`                     | `10`                    |