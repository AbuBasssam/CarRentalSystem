# 🧪 API Test Cases — Set Primary Car Image

> **Project:** Car Rental API — C# / ASP.NET Core
> **Testing Tool:** Postman (Manual)
> **Total Cases:** 8 test cases
> **Endpoint:** `PATCH {{baseUrl}}/api/v1/cars/{{carId}}/images/{{imageId}}/primary`

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

## 🗺️ What This Endpoint Does

Promotes a specific car image to **Primary**, and automatically demotes the current Primary image.

**Business Rules (from Handler + `CarImage.SetAsPrimary()`):**
- Image must exist on the specified car **and** not be soft-deleted → otherwise `404`
- Image must not already be deleted → otherwise `400` (`"Cannot set a deleted image as primary."`)
- If the image **is already Primary** → silent success, `200 OK`, no DB change
- The previous Primary image is automatically demoted (`IsPrimary=false`)
- No body is required — both `CarId` and `ImageId` come from the URL

---

## Summary

| # | Test Case | Type | Expected Status Code |
|---|-----------|------|----------------------|
| TC-01 | Set a non-primary image as primary | ✅ Happy Path | `200 OK` |
| TC-02 | Image is already primary | ⚠️ Edge Case | `200 OK` |
| TC-03 | Car does not exist | ❌ Error Case | `404 Not Found` |
| TC-04 | Image does not exist on this car | ❌ Error Case | `404 Not Found` |
| TC-05 | Image is soft-deleted | ❌ Error Case | `404 Not Found` |
| TC-06 | ImageId belongs to a different car | ❌ Error Case | `404 Not Found` |
| TC-07 | Image is soft-deleted but found via direct Id (guard check) | ❌ Error Case | `404 Not Found` |
| TC-08 | Set primary with valid Cookie but Customer role | 🔐 Auth Case | `403 Forbidden` |

---

## Test Case Details

---

### ✅ TC-01 — Set a non-primary image as primary

**Pre-conditions:**
- Car with `Id=15` exists
- Image with `Id=2` exists on this car, `IsDeleted=false`, `IsPrimary=false`
- Image with `Id=1` currently has `IsPrimary=true`

**Postman Setup:**
```
Method : PATCH
URL    : {{baseUrl}}/api/v1/cars/15/images/2/primary
Body   : none
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

> 💡 `SetAsPrimary()` calls `currentPrimary.Demote()` on `Id=1` (sets `IsPrimary=false`), then sets `Id=2` to `IsPrimary=true`.
> **Verify in DB after this request:** `Id=1` should have `IsPrimary=false` and `Id=2` should have `IsPrimary=true`.

---

### ⚠️ TC-02 — Image is already primary

**Pre-conditions:**
- Car with `Id=15` exists
- Image with `Id=1` exists, `IsDeleted=false`, `IsPrimary=true` (already the primary)

**Postman Setup:**
```
Method : PATCH
URL    : {{baseUrl}}/api/v1/cars/15/images/1/primary
Body   : none
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

> 💡 `SetAsPrimary()` checks `if (IsPrimary) return (true, null)` — it exits silently without making any changes. `SaveChangesAsync()` runs with no diff. The operation is **idempotent**.
> **No DB change occurs** — this is confirmed from `CarImage.SetAsPrimary()`.

---

### ❌ TC-03 — Car does not exist

**Pre-conditions:** No car with `Id=9999` exists in the database.

**Postman Setup:**
```
Method : PATCH
URL    : {{baseUrl}}/api/v1/cars/9999/images/1/primary
Body   : none
```

**Expected Response — `404 Not Found`:**
```json
{
  "statusCode": 404,
  "succeeded": false,
  "message": "Image not found.",
  "errors": ["Image not found."]
}
```

> 💡 The Handler queries images filtered by both `img.CarId == request.CarId` and `img.Id == request.ImageId`. Since no car with `Id=9999` exists, no image matches → `targetImage` is `null` → `404`.
> Note: the error message is `"Image not found."` not `"Car not found."` — the Handler does not check for the car separately.

---

### ❌ TC-04 — Image does not exist on this car

**Pre-conditions:**
- Car with `Id=15` exists
- No image with `Id=9999` exists on this car

**Postman Setup:**
```
Method : PATCH
URL    : {{baseUrl}}/api/v1/cars/15/images/9999/primary
Body   : none
```

**Expected Response — `404 Not Found`:**
```json
{
  "statusCode": 404,
  "succeeded": false,
  "message": "Image not found.",
  "errors": ["Image not found."]
}
```

---

### ❌ TC-05 — Image is soft-deleted

**Pre-conditions:**
- Car with `Id=15` exists
- Image with `Id=3` exists in DB but `IsDeleted=true`

**Postman Setup:**
```
Method : PATCH
URL    : {{baseUrl}}/api/v1/cars/15/images/3/primary
Body   : none
```

**Expected Response — `404 Not Found`:**
```json
{
  "statusCode": 404,
  "succeeded": false,
  "message": "Image not found.",
  "errors": ["Image not found."]
}
```

> 💡 The Handler's query includes `&& !img.IsDeleted` — a soft-deleted image is treated the same as a non-existent one. The `SetAsPrimary()` guard `if (IsDeleted) return (false, ...)` is never even reached since the image is filtered out at the query level.

---

### ❌ TC-06 — ImageId belongs to a different car

**Pre-conditions:**
- Car with `Id=15` exists
- Image with `Id=5` exists but belongs to `CarId=16`, not `CarId=15`

**Postman Setup:**
```
Method : PATCH
URL    : {{baseUrl}}/api/v1/cars/15/images/5/primary
Body   : none
```

**Expected Response — `404 Not Found`:**
```json
{
  "statusCode": 404,
  "succeeded": false,
  "message": "Image not found.",
  "errors": ["Image not found."]
}
```

> 💡 The query filters by both `img.Id == request.ImageId && img.CarId == request.CarId`. Since `Id=5` belongs to `CarId=16`, the filter returns no result → `404`. Cross-car promotion is impossible by design.

---

### ❌ TC-07 — Deleted image passed with matching CarId (double guard test)

**Description:** Verify that a soft-deleted image is rejected even when the `CarId` is correct. This tests both the query-level filter and the `SetAsPrimary()` guard together.

**Pre-conditions:**
- Car with `Id=15` exists
- Image with `Id=3` exists on `CarId=15` but `IsDeleted=true`

**Postman Setup:**
```
Method : PATCH
URL    : {{baseUrl}}/api/v1/cars/15/images/3/primary
Body   : none
```

**Expected Response — `404 Not Found`:**
```json
{
  "statusCode": 404,
  "succeeded": false,
  "message": "Image not found.",
  "errors": ["Image not found."]
}
```

> 💡 This is functionally identical to TC-05 and exists to confirm the behavior is consistent when `CarId` is correct but `IsDeleted=true`. The `!img.IsDeleted` filter in the query rejects it before `SetAsPrimary()` is ever called.

---

### 🔐 TC-08 — Set primary with valid Cookie but Customer role

> **TODO:** Enable after removing the comment from `[Authorize(Roles = Roles.Admin)]`.

**Description:** Cookie is valid but the user's role is `Customer` not `Admin`.

**How to test it:**
1. Sign in with a regular Customer account → `POST /api/v1/authentication/signin`
2. Send the PATCH request — Postman will send the Cookie automatically

**Postman Setup:**
```
Method : PATCH
URL    : {{baseUrl}}/api/v1/cars/15/images/2/primary
Body   : none
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
1. Does the image exist on this car
   and is IsDeleted=false?           → No  : 404 Not Found ("Image not found.")
                                     → Yes : continue
2. Is the image already Primary?     → Yes : return Success(true) — no DB change
                                     → No  : continue
3. Find current Primary image
   (same CarId, IsPrimary=true,
   IsDeleted=false)
4. Demote current Primary (IsPrimary = false)
5. Promote target image  (IsPrimary = true)
6. SaveChanges → return 200 OK
```

### Key Behaviors to Remember

| Behavior | Detail |
|----------|--------|
| No request body needed | Both IDs come from the URL only |
| Already-primary image | Silent success — idempotent, no DB change |
| Deleted image | Filtered out at query level — returns `404`, not `400` |
| Cross-car promotion | Impossible — query always filters by `CarId` |
| Car not found | Returns `404 "Image not found."` — no separate car check in the Handler |
| Previous primary | Automatically demoted via `Demote()` → `IsPrimary=false` |