# 🧪 API Test Cases — Get Car Primary Image

> **Project:** Car Rental API — C# / ASP.NET Core
> **Testing Tool:** Postman (Manual)
> **Total Cases:** 7 test cases
> **Endpoint:** `GET {{baseUrl}}/api/v1/cars/{{carId}}/images/primary`

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

Serves the car's **Primary image as a binary file stream** (`image/webp`) directly — not as a JSON response.

**What makes this endpoint different from others:**
- The response is **not JSON** — it is a raw binary file (`FileContentResult`)
- On success, Postman displays the image directly in the response body
- On failure, the response falls back to the standard JSON envelope

**Lookup logic (from Handler):**
```
WHERE CarId = {id} AND IsPrimary = true AND IsDeleted = false
```

> 💡 **All checks run in a single query:** `CarId` match + `IsPrimary` + `!IsDeleted` + `car.IsActive` + `branch.IsActive` — EF Core resolves the joins automatically. If any condition fails, `fileName` is `null` and the response is `404`.

---

## Summary

| # | Test Case | Type | Expected Status Code |
|---|-----------|------|----------------------|
| TC-01 | Car exists and has a primary image | ✅ Happy Path | `200 OK` (binary stream) |
| TC-02 | Car does not exist | ❌ Error Case | `404 Not Found` |
| TC-03 | Car exists but has no primary image | ❌ Error Case | `404 Not Found` |
| TC-04 | Car exists but all images are soft-deleted | ❌ Error Case | `404 Not Found` |
| TC-05 | Car exists but `IsActive=false` | ⚠️ Edge Case | `404 Not Found` |
| TC-06 | Car's branch is inactive (`IsActive=false`) | ⚠️ Edge Case | `404 Not Found` |
| TC-07 | Primary image file is missing from disk | ⚠️ Edge Case | `500 Internal Server Error` |

---

## Test Case Details

---

### ✅ TC-01 — Car exists and has a primary image

**Pre-conditions:**
- Car with `Id=15` exists, `IsActive=true`
- Branch of car `Id=15` is active (`IsActive=true`)
- At least one image exists with `IsPrimary=true` and `IsDeleted=false`
- The image file exists on disk at the expected path

**Postman Setup:**
```
Method : GET
URL    : {{baseUrl}}/api/v1/cars/15/images/primary
Body   : none
```

**Expected Response — `200 OK`:**
- **Not JSON** — the response body is a raw binary image
- Response Header: `Content-Type: image/webp`
- Postman will render the image visually in the response panel

> 💡 In Postman, switch to the **Body** tab after the request and select **Preview** to see the image rendered. The file is served directly via `File(result.Data.Content, result.Data.ContentType)` from the Controller — not wrapped in the standard JSON envelope.

---

### ❌ TC-02 — Car does not exist

**Pre-conditions:** No car with `Id=9999` exists in the database.

**Postman Setup:**
```
Method : GET
URL    : {{baseUrl}}/api/v1/cars/9999/images/primary
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

> 💡 The Handler finds no image matching `CarId=9999 && IsPrimary=true && !IsDeleted` → `fileName` is `null` → `_responseHandler.NotFound<CarImageFileDto>()`. Note the response has no custom message — `NotFound` is called without a reason string here.

---

### ❌ TC-03 — Car exists but has no primary image

**Pre-conditions:**
- Car with `Id=15` exists
- The car has images but **none** has `IsPrimary=true` (e.g. all were demoted or none was ever set)

**Postman Setup:**
```
Method : GET
URL    : {{baseUrl}}/api/v1/cars/15/images/primary
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

> 💡 Same result as TC-02 — the query finds no row where `IsPrimary=true`, so `fileName` is `null`. This state should not occur in normal flow since the upload handler always sets the first image as Primary, and the delete handler promotes the next image automatically. However, it can happen via direct DB manipulation or edge cases.

---

### ❌ TC-04 — Car exists but all images are soft-deleted

**Pre-conditions:**
- Car with `Id=15` exists
- All images on this car have `IsDeleted=true`

**Postman Setup:**
```
Method : GET
URL    : {{baseUrl}}/api/v1/cars/15/images/primary
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

> 💡 The query filter `!img.IsDeleted` excludes all images → `fileName` is `null` → `404`. Same response as TC-02 and TC-03.

---

### ⚠️ TC-05 — Car exists but `IsActive=false`

**Pre-conditions:**
- Car with `Id=15` exists but `IsActive=false`
- The car has a valid non-deleted Primary image

**Postman Setup:**
```
Method : GET
URL    : {{baseUrl}}/api/v1/cars/15/images/primary
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

> 💡 The Handler query includes `&& img.Car.IsActive` — EF Core joins the `Cars` table automatically. Since `IsActive=false`, no row matches and `fileName` is `null` → `404`. Same response shape as TC-02, TC-03, and TC-04.

---

### ⚠️ TC-06 — Car's branch is inactive (`IsActive=false`)

**Pre-conditions:**
- Car with `Id=15` exists and `IsActive=true`
- The car's current branch has `IsActive=false`
- The car has a valid non-deleted Primary image

**Postman Setup:**
```
Method : GET
URL    : {{baseUrl}}/api/v1/cars/15/images/primary
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

> 💡 The Handler query includes `&& img.Car.CurrentBranch.IsActive` — EF Core joins the `Branches` table automatically. Since the branch is inactive, no row matches and `fileName` is `null` → `404`. Same response shape as TC-02, TC-03, and TC-04.

---

### ⚠️ TC-07 — Primary image file is missing from disk

**Description:** The DB record exists and `IsPrimary=true`, but the actual `.webp` file has been deleted from the filesystem (e.g. manual deletion or storage failure).

**Pre-conditions:**
- Car with `Id=15` exists and has a primary image record in DB
- The `.webp` file referenced by `FileName` does **not** exist on disk

**Postman Setup:**
```
Method : GET
URL    : {{baseUrl}}/api/v1/cars/15/images/primary
Body   : none
```

**Expected Response — `500 Internal Server Error`:**
```json
{
  "statusCode": 500,
  "succeeded": false,
  "message": "An unexpected error occurred.",
  "errors": ["An unexpected error occurred."]
}
```

> 💡 `_fileStorage.GetCarImageAsync(fileName, ...)` will throw when the file is not found on disk. The Handler catches all exceptions via `catch (Exception ex)` and returns `InternalServerError`. This is a storage consistency issue — the DB and disk are out of sync.
> **This state is hard to reproduce in testing** — requires manually deleting a file from `storage/cars/{carId}/` on the server.

---

---

## 📋 Quick Reference

### Response Format — Important Difference

| Scenario | Response Format |
|----------|----------------|
| ✅ Success | Raw binary `image/webp` — **not JSON** |
| ❌ Any failure | Standard JSON envelope `{ statusCode, succeeded, ... }` |

### Business Rule Execution Order (inside the Handler)

```
1. Single query:
   CarId match + IsPrimary=true + IsDeleted=false
   + car.IsActive=true + branch.IsActive=true
   (EF Core resolves joins automatically)
                                        → No match : 404 Not Found
                                        → Match    : fileName retrieved
2. Load file from disk via IFileStorageService
                                        → File missing : 500 (exception caught)
                                        → File found   : continue
3. Return binary stream → 200 OK (image/webp)
```

### How to Verify the Image in Postman

```
1. Send the GET request
2. In the Response panel → click Body tab
3. Select "Preview" mode
4. The image renders directly — no download needed
```

### All `404` Cases Return the Same Response

| TC | Reason for 404 |
|----|---------------|
| TC-02 | Car does not exist |
| TC-03 | Car has no primary image |
| TC-04 | All images are soft-deleted |
| TC-05 | Car is inactive (`car.IsActive=false` filter in query) |
| TC-06 | Branch is inactive (`branch.IsActive=false` filter in query) |# 🧪 API Test Cases — Get Car Primary Image

> **Project:** Car Rental API — C# / ASP.NET Core
> **Testing Tool:** Postman (Manual)
> **Total Cases:** 7 test cases
> **Endpoint:** `GET {{baseUrl}}/api/v1/cars/{{carId}}/images/primary`

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

Serves the car's **Primary image as a binary file stream** (`image/webp`) directly — not as a JSON response.

**What makes this endpoint different from others:**
- The response is **not JSON** — it is a raw binary file (`FileContentResult`)
- On success, Postman displays the image directly in the response body
- On failure, the response falls back to the standard JSON envelope

**Lookup logic (from Handler):**
```
WHERE CarId = {id} AND IsPrimary = true AND IsDeleted = false
```

> 💡 **All checks run in a single query:** `CarId` match + `IsPrimary` + `!IsDeleted` + `car.IsActive` + `branch.IsActive` — EF Core resolves the joins automatically. If any condition fails, `fileName` is `null` and the response is `404`.

---

## Summary

| # | Test Case | Type | Expected Status Code |
|---|-----------|------|----------------------|
| TC-01 | Car exists and has a primary image | ✅ Happy Path | `200 OK` (binary stream) |
| TC-02 | Car does not exist | ❌ Error Case | `404 Not Found` |
| TC-03 | Car exists but has no primary image | ❌ Error Case | `404 Not Found` |
| TC-04 | Car exists but all images are soft-deleted | ❌ Error Case | `404 Not Found` |
| TC-05 | Car exists but `IsActive=false` | ⚠️ Edge Case | `404 Not Found` |
| TC-06 | Car's branch is inactive (`IsActive=false`) | ⚠️ Edge Case | `404 Not Found` |
| TC-07 | Primary image file is missing from disk | ⚠️ Edge Case | `500 Internal Server Error` |

---

## Test Case Details

---

### ✅ TC-01 — Car exists and has a primary image

**Pre-conditions:**
- Car with `Id=15` exists, `IsActive=true`
- Branch of car `Id=15` is active (`IsActive=true`)
- At least one image exists with `IsPrimary=true` and `IsDeleted=false`
- The image file exists on disk at the expected path

**Postman Setup:**
```
Method : GET
URL    : {{baseUrl}}/api/v1/cars/15/images/primary
Body   : none
```

**Expected Response — `200 OK`:**
- **Not JSON** — the response body is a raw binary image
- Response Header: `Content-Type: image/webp`
- Postman will render the image visually in the response panel

> 💡 In Postman, switch to the **Body** tab after the request and select **Preview** to see the image rendered. The file is served directly via `File(result.Data.Content, result.Data.ContentType)` from the Controller — not wrapped in the standard JSON envelope.

---

### ❌ TC-02 — Car does not exist

**Pre-conditions:** No car with `Id=9999` exists in the database.

**Postman Setup:**
```
Method : GET
URL    : {{baseUrl}}/api/v1/cars/9999/images/primary
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

> 💡 The Handler finds no image matching `CarId=9999 && IsPrimary=true && !IsDeleted` → `fileName` is `null` → `_responseHandler.NotFound<CarImageFileDto>()`. Note the response has no custom message — `NotFound` is called without a reason string here.

---

### ❌ TC-03 — Car exists but has no primary image

**Pre-conditions:**
- Car with `Id=15` exists
- The car has images but **none** has `IsPrimary=true` (e.g. all were demoted or none was ever set)

**Postman Setup:**
```
Method : GET
URL    : {{baseUrl}}/api/v1/cars/15/images/primary
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

> 💡 Same result as TC-02 — the query finds no row where `IsPrimary=true`, so `fileName` is `null`. This state should not occur in normal flow since the upload handler always sets the first image as Primary, and the delete handler promotes the next image automatically. However, it can happen via direct DB manipulation or edge cases.

---

### ❌ TC-04 — Car exists but all images are soft-deleted

**Pre-conditions:**
- Car with `Id=15` exists
- All images on this car have `IsDeleted=true`

**Postman Setup:**
```
Method : GET
URL    : {{baseUrl}}/api/v1/cars/15/images/primary
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

> 💡 The query filter `!img.IsDeleted` excludes all images → `fileName` is `null` → `404`. Same response as TC-02 and TC-03.

---

### ⚠️ TC-05 — Car exists but `IsActive=false`

**Pre-conditions:**
- Car with `Id=15` exists but `IsActive=false`
- The car has a valid non-deleted Primary image

**Postman Setup:**
```
Method : GET
URL    : {{baseUrl}}/api/v1/cars/15/images/primary
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

> 💡 The Handler query includes `&& img.Car.IsActive` — EF Core joins the `Cars` table automatically. Since `IsActive=false`, no row matches and `fileName` is `null` → `404`. Same response shape as TC-02, TC-03, and TC-04.

---

### ⚠️ TC-06 — Car's branch is inactive (`IsActive=false`)

**Pre-conditions:**
- Car with `Id=15` exists and `IsActive=true`
- The car's current branch has `IsActive=false`
- The car has a valid non-deleted Primary image

**Postman Setup:**
```
Method : GET
URL    : {{baseUrl}}/api/v1/cars/15/images/primary
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

> 💡 The Handler query includes `&& img.Car.CurrentBranch.IsActive` — EF Core joins the `Branches` table automatically. Since the branch is inactive, no row matches and `fileName` is `null` → `404`. Same response shape as TC-02, TC-03, and TC-04.

---

### ⚠️ TC-07 — Primary image file is missing from disk

**Description:** The DB record exists and `IsPrimary=true`, but the actual `.webp` file has been deleted from the filesystem (e.g. manual deletion or storage failure).

**Pre-conditions:**
- Car with `Id=15` exists and has a primary image record in DB
- The `.webp` file referenced by `FileName` does **not** exist on disk

**Postman Setup:**
```
Method : GET
URL    : {{baseUrl}}/api/v1/cars/15/images/primary
Body   : none
```

**Expected Response — `500 Internal Server Error`:**
```json
{
  "statusCode": 500,
  "succeeded": false,
  "message": "An unexpected error occurred.",
  "errors": ["An unexpected error occurred."]
}
```

> 💡 `_fileStorage.GetCarImageAsync(fileName, ...)` will throw when the file is not found on disk. The Handler catches all exceptions via `catch (Exception ex)` and returns `InternalServerError`. This is a storage consistency issue — the DB and disk are out of sync.
> **This state is hard to reproduce in testing** — requires manually deleting a file from `storage/cars/{carId}/` on the server.

---

---

## 📋 Quick Reference

### Response Format — Important Difference

| Scenario | Response Format |
|----------|----------------|
| ✅ Success | Raw binary `image/webp` — **not JSON** |
| ❌ Any failure | Standard JSON envelope `{ statusCode, succeeded, ... }` |

### Business Rule Execution Order (inside the Handler)

```
1. Single query:
   CarId match + IsPrimary=true + IsDeleted=false
   + car.IsActive=true + branch.IsActive=true
   (EF Core resolves joins automatically)
                                        → No match : 404 Not Found
                                        → Match    : fileName retrieved
2. Load file from disk via IFileStorageService
                                        → File missing : 500 (exception caught)
                                        → File found   : continue
3. Return binary stream → 200 OK (image/webp)
```

### How to Verify the Image in Postman

```
1. Send the GET request
2. In the Response panel → click Body tab
3. Select "Preview" mode
4. The image renders directly — no download needed
```

### All `404` Cases Return the Same Response

| TC | Reason for 404 |
|----|---------------|
| TC-02 | Car does not exist |
| TC-03 | Car has no primary image |
| TC-04 | All images are soft-deleted |
| TC-05 | Car is inactive (`car.IsActive=false` filter in query) |
| TC-06 | Branch is inactive (`branch.IsActive=false` filter in query) |