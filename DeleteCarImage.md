# 🧪 API Test Cases — Delete Car Image

> **Project:** Car Rental API — C# / ASP.NET Core
> **Testing Tool:** Postman (Manual)
> **Total Cases:** 8 test cases
> **Endpoint:** `DELETE {{baseUrl}}/api/v1/cars/{{carId}}/images/{{imageId}}`

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

Soft-deletes a car image by marking it as deleted in the database — **the file is not removed from disk**.

**Business Rules (from Handler + `Car.RemoveImage()`):**
- Car must exist, otherwise `404`
- Image must exist on that car **and** not already be deleted, otherwise `400`
- If the deleted image was the **Primary**, the next available image (lowest `Id`) is automatically promoted to Primary
- If the deleted image is Primary and **no other images exist**, the deletion is **blocked** → `400 Bad Request`

---

## Summary

| # | Test Case | Type | Expected Status Code |
|---|-----------|------|----------------------|
| TC-01 | Delete a non-primary image | ✅ Happy Path | `200 OK` |
| TC-02 | Delete the primary image (another image exists) | ✅ Happy Path | `200 OK` |
| TC-03 | Car does not exist | ❌ Error Case | `404 Not Found` |
| TC-04 | Image does not exist on this car | ❌ Error Case | `400 Bad Request` |
| TC-05 | Image is already soft-deleted | ❌ Error Case | `400 Bad Request` |
| TC-06 | ImageId belongs to a different car | ❌ Error Case | `400 Bad Request` |
| TC-07 | Delete the only image on a car (last image) | ⚠️ Edge Case | `400 Bad Request` |
| TC-08 | Delete with valid Cookie but Customer role | 🔐 Auth Case | `403 Forbidden` |

---

## Test Case Details

---

### ✅ TC-01 — Delete a non-primary image

**Pre-conditions:**
- Car with `Id=15` exists
- Image with `Id=3` exists on this car, `IsDeleted=false`, `IsPrimary=false`
- At least one other non-deleted image exists on the car

**Postman Setup:**
```
Method : DELETE
URL    : {{baseUrl}}/api/v1/cars/15/images/3
Body   : none
```

**Expected Response — `200 OK`:**
```json
{
  "statusCode": 200,
  "succeeded": true,
  "message": "Deleted",
  "data": true
}
```

> 💡 Since this image is not Primary, no promotion happens. The image is soft-deleted (`IsDeleted=true`) — it remains in the database and the file stays on disk.

---

### ✅ TC-02 — Delete the primary image (another image exists)

**Pre-conditions:**
- Car with `Id=15` exists
- Image with `Id=1` exists, `IsDeleted=false`, `IsPrimary=true`
- At least one other non-deleted image exists (e.g. `Id=2`, `Id=3`)

**Postman Setup:**
```
Method : DELETE
URL    : {{baseUrl}}/api/v1/cars/15/images/1
Body   : none
```

**Expected Response — `200 OK`:**
```json
{
  "statusCode": 200,
  "succeeded": true,
  "message": "Deleted",
  "data": true
}
```

> 💡 `RemoveImage()` detects the deleted image is Primary and promotes the next available image (lowest `Id` among non-deleted) automatically.
> **Verify in DB after this request:** `Id=2` should have `IsPrimary=true`.

---

### ❌ TC-03 — Car does not exist

**Pre-conditions:** No car with `Id=9999` exists in the database.

**Postman Setup:**
```
Method : DELETE
URL    : {{baseUrl}}/api/v1/cars/9999/images/1
Body   : none
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

### ❌ TC-04 — Image does not exist on this car

**Pre-conditions:**
- Car with `Id=15` exists
- No image with `Id=9999` exists on this car

**Postman Setup:**
```
Method : DELETE
URL    : {{baseUrl}}/api/v1/cars/15/images/9999
Body   : none
```

**Expected Response — `400 Bad Request`:**
```json
{
  "statusCode": 400,
  "succeeded": false,
  "message": "Image not found.",
  "errors": ["Image not found."]
}
```

> 💡 `RemoveImage()` returns `(false, "Image not found.")` when `.FirstOrDefault(i => i.Id == imageId && !i.IsDeleted)` finds nothing. The Handler forwards this reason directly to `BadRequest`.

---

### ❌ TC-05 — Image is already soft-deleted

**Pre-conditions:**
- Car with `Id=15` exists
- Image with `Id=3` exists in DB but `IsDeleted=true` (deleted in a previous request)

**Postman Setup:**
```
Method : DELETE
URL    : {{baseUrl}}/api/v1/cars/15/images/3
Body   : none
```

**Expected Response — `400 Bad Request`:**
```json
{
  "statusCode": 400,
  "succeeded": false,
  "message": "Image not found.",
  "errors": ["Image not found."]
}
```

> 💡 The query filters `!i.IsDeleted` — a previously deleted image is treated identically to a non-existent one. Same error message as TC-04.

---

### ❌ TC-06 — ImageId belongs to a different car

**Pre-conditions:**
- Car with `Id=15` exists
- Image with `Id=5` exists but belongs to `CarId=16`, not `CarId=15`

**Postman Setup:**
```
Method : DELETE
URL    : {{baseUrl}}/api/v1/cars/15/images/5
Body   : none
```

**Expected Response — `400 Bad Request`:**
```json
{
  "statusCode": 400,
  "succeeded": false,
  "message": "Image not found.",
  "errors": ["Image not found."]
}
```

> 💡 The Handler loads the car by `CarId=15`, then calls `RemoveImage(5)` on that car's own `Images` collection. Since `Id=5` is not in that collection, `FirstOrDefault` returns `null` — same result as TC-04.

---

### ⚠️ TC-07 — Delete the only image on a car (last image)

**Pre-conditions:**
- Car with `Id=15` exists
- Exactly **one** image exists: `Id=1`, `IsDeleted=false`, `IsPrimary=true`
- No other non-deleted images exist for this car

**Postman Setup:**
```
Method : DELETE
URL    : {{baseUrl}}/api/v1/cars/15/images/1
Body   : none
```

**Expected Response — `400 Bad Request`:**
```json
{
  "statusCode": 400,
  "succeeded": false,
  "message": "Cannot delete the primary image when it is the only image.",
  "errors": ["Cannot delete the primary image when it is the only image."]
}
```

> 💡 **Confirmed from `CarImage.Delete()`:** When the image is Primary and `nextPrimary` is `null`, the method immediately returns `(false, "Cannot delete the primary image when it is the only image.")` without making any changes. The Handler receives `IsSuccess=false` and calls `BadRequest` with that reason.
> The image is **not** deleted — `IsDeleted` remains `false`.

---

### 🔐 TC-08 — Delete with valid Cookie but Customer role

> **TODO:** Enable after removing the comment from `[Authorize(Roles = Roles.Admin)]`.

**Description:** Cookie is valid but the user's role is `Customer` not `Admin`.

**How to test it:**
1. Sign in with a regular Customer account → `POST /api/v1/authentication/signin`
2. Send the DELETE request — Postman will send the Cookie automatically

**Postman Setup:**
```
Method : DELETE
URL    : {{baseUrl}}/api/v1/cars/15/images/1
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
1. Does the car exist?                        → No  : 404 Not Found
                                              → Yes : continue
2. Does the image exist on this car
   and is IsDeleted=false?                    → No  : 400 Bad Request ("Image not found.")
                                              → Yes : continue
3. Is the image the Primary?
   → Yes : find next image (lowest Id, not deleted) → promote to Primary
   → No  : skip promotion
4. Soft-delete the image (IsDeleted = true)
5. SaveChanges → return 200 OK (Deleted)
```

### Key Behaviors to Remember

| Behavior | Detail |
|----------|--------|
| Deletion type | Soft delete only — `IsDeleted=true`, file stays on disk |
| Already-deleted image | Treated as "not found" — same `400` as a missing image |
| Cross-car image access | Not possible — images are scoped to the car's own collection |
| Primary promotion | Automatic on primary deletion — next image by lowest `Id` |
| Last image deletion | Blocked — `CarImage.Delete()` returns `(false, ...)` when image is the only Primary → `400 Bad Request` |