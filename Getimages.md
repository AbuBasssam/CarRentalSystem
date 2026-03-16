# 🧪 API Test Cases — Get Car Images

> **Project:** Car Rental API — C# / ASP.NET Core
> **Testing Tool:** Postman (Manual)
> **Total Cases:** 6 test cases
> **Endpoint:** `GET {{baseUrl}}/api/v1/cars/{{carId}}/images`

---

## 📖 Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Happy Path — expected successful flow |
| ❌ | Error Case — expected failure |
| ⚠️ | Edge Case — boundary or rare condition |
| `TC-XX` | Test case number |

> 🔓 **No Auth Required** — this endpoint is on `PublicCarsController` and has no `[Authorize]` attribute.

---

## 🗺️ What This Endpoint Does

Returns a **JSON list of image metadata** for a given car — each entry contains the image `Id`, a serving `URL`, and whether the image `IsPrimary`.

**What makes this endpoint different from GetPrimaryImage:**
- The response **is JSON** — not a binary stream
- Returns **all non-deleted images** for the car, not just the primary one
- Each item in the list includes a pre-built serving URL in the format:
  `{{baseUrl}}/api/v1/cars/{carId}/images/{imageId}`

**Lookup logic (from Handler):**
```
WHERE CarId = {id} AND IsDeleted = false
```

> 💡 **All checks run in a single query:** `CarId` match + `!IsDeleted` + `car.IsActive` + `branch.IsActive` — EF Core resolves the joins automatically. If any condition fails, the list is empty and the response is `404`.

---

## Summary

| # | Test Case | Type | Expected Status Code |
|---|-----------|------|----------------------|
| TC-01 | Car exists and has multiple images | ✅ Happy Path | `200 OK` (JSON list) |
| TC-02 | Car exists and has exactly one image | ✅ Happy Path | `200 OK` (JSON list, one item) |
| TC-03 | Car does not exist | ❌ Error Case | `404 Not Found` |
| TC-04 | Car exists but all images are soft-deleted | ❌ Error Case | `404 Not Found` |
| TC-05 | Car exists but `IsActive=false` | ⚠️ Edge Case | `404 Not Found` |
| TC-06 | Car's branch is inactive (`IsActive=false`) | ⚠️ Edge Case | `404 Not Found` |

---

## Test Case Details

---

### ✅ TC-01 — Car exists and has multiple images

**Pre-conditions:**
- Car with `Id=15` exists, `IsActive=true`
- Branch of car `Id=15` is active (`IsActive=true`)
- At least two images exist with `IsDeleted=false`
- At least one of them has `IsPrimary=true`

**Postman Setup:**
```
Method : GET
URL    : {{baseUrl}}/api/v1/cars/15/images
Body   : none
```

**Expected Response — `200 OK`:**
```json
{
  "statusCode": 200,
  "succeeded": true,
  "message": null,
  "errors": null,
  "data": [
    {
      "id": 3,
      "url": "{{baseUrl}}/api/v1/cars/15/images/3",
      "isPrimary": true
    },
    {
      "id": 7,
      "url": "{{baseUrl}}/api/v1/cars/15/images/7",
      "isPrimary": false
    }
  ]
}
```

> 💡 Results are ordered by `img.Id` ascending (as defined in the Handler's `.OrderBy(img => img.Id)`). The primary image is **not** guaranteed to be first — it is wherever its `Id` falls in ascending order. Verify that exactly one item has `"isPrimary": true` and the rest have `false`.

---

### ✅ TC-02 — Car exists and has exactly one image

**Pre-conditions:**
- Car with `Id=15` exists, `IsActive=true`
- Branch of car `Id=15` is active (`IsActive=true`)
- Exactly one image exists with `IsDeleted=false` and `IsPrimary=true`

**Postman Setup:**
```
Method : GET
URL    : {{baseUrl}}/api/v1/cars/15/images
Body   : none
```

**Expected Response — `200 OK`:**
```json
{
  "statusCode": 200,
  "succeeded": true,
  "message": null,
  "errors": null,
  "data": [
    {
      "id": 3,
      "url": "{{baseUrl}}/api/v1/cars/15/images/3",
      "isPrimary": true
    }
  ]
}
```

> 💡 A single-item list is a valid success response. The only image must have `"isPrimary": true` since the upload handler always marks the first image as primary.

---

### ❌ TC-03 — Car does not exist

**Pre-conditions:** No car with `Id=9999` exists in the database.

**Postman Setup:**
```
Method : GET
URL    : {{baseUrl}}/api/v1/cars/9999/images
Body   : none
```

**Expected Response — `404 Not Found`:**
```json
{
  "statusCode": 404,
  "succeeded": false,
  "message": null,
  "errors": null
}
```

> 💡 The Handler query returns an empty list because no `CarImage` row matches `CarId=9999`. The Handler checks `!images.Any()` and returns `_responseHandler.NotFound<List<CarImageMetadataDto>>()`.

---

### ❌ TC-04 — Car exists but all images are soft-deleted

**Pre-conditions:**
- Car with `Id=15` exists, `IsActive=true`
- Branch is active (`IsActive=true`)
- All images on this car have `IsDeleted=true`

**Postman Setup:**
```
Method : GET
URL    : {{baseUrl}}/api/v1/cars/15/images
Body   : none
```

**Expected Response — `404 Not Found`:**
```json
{
  "statusCode": 404,
  "succeeded": false,
  "message": null,
  "errors": null
}
```

> 💡 The query filter `!img.IsDeleted` excludes all images → the list is empty → `404`. Same response shape as TC-03.

---

### ⚠️ TC-05 — Car exists but `IsActive=false`

**Pre-conditions:**
- Car with `Id=15` exists but `IsActive=false`
- The car has at least one valid non-deleted image

**Postman Setup:**
```
Method : GET
URL    : {{baseUrl}}/api/v1/cars/15/images
Body   : none
```

**Expected Response — `404 Not Found`:**
```json
{
  "statusCode": 404,
  "succeeded": false,
  "message": null,
  "errors": null
}
```

> 💡 The Handler query includes `&& img.Car.IsActive` — EF Core joins the `Cars` table automatically. Since `IsActive=false`, no row matches and the list is empty → `404`. Same response shape as TC-03 and TC-04.

---

### ⚠️ TC-06 — Car's branch is inactive (`IsActive=false`)

**Pre-conditions:**
- Car with `Id=15` exists and `IsActive=true`
- The car's current branch has `IsActive=false`
- The car has at least one valid non-deleted image

**Postman Setup:**
```
Method : GET
URL    : {{baseUrl}}/api/v1/cars/15/images
Body   : none
```

**Expected Response — `404 Not Found`:**
```json
{
  "statusCode": 404,
  "succeeded": false,
  "message": null,
  "errors": null
}
```

> 💡 The Handler query includes `&& img.Car.CurrentBranch.IsActive` — EF Core joins the `Branches` table automatically. Since the branch is inactive, no row matches and the list is empty → `404`. Same response shape as TC-03, TC-04, and TC-05.

---

---

## 📋 Quick Reference

### Response Format

| Scenario | Response Format |
|----------|----------------|
| ✅ Success | Standard JSON envelope with `data` array |
| ❌ Any failure | Standard JSON envelope `{ statusCode, succeeded, ... }` with no `data` |

> Unlike `GetPrimaryImage`, **both success and failure responses are JSON** — there is no binary stream involved.

### Business Rule Execution Order (inside the Handler)

```
1. Single query:
   CarId match + IsDeleted=false
   + car.IsActive=true + branch.IsActive=true
   (EF Core resolves joins automatically)
                                        → Empty list : 404 Not Found
                                        → Non-empty  : list of CarImageMetadataDto
2. Return JSON list → 200 OK
```

### All `404` Cases Return the Same Response

| TC | Reason for 404 |
|----|---------------|
| TC-03 | Car does not exist |
| TC-04 | All images are soft-deleted |
| TC-05 | Car is inactive (`car.IsActive=false` filter in query) |
| TC-06 | Branch is inactive (`branch.IsActive=false` filter in query) |

### Key Differences vs. `GetPrimaryImage`

| | `GetImages` | `GetPrimaryImage` |
|---|---|---|
| **Response on success** | JSON list | Binary `image/webp` stream |
| **Returns** | All non-deleted images | Only the primary image |
| **Filter** | `!IsDeleted` | `IsPrimary=true AND !IsDeleted` |
| **TC-07 (disk missing)** | ❌ Not applicable — no file I/O | ✅ Applicable — reads from disk |
| **Order** | `ORDER BY Id ASC` | N/A (single result) |