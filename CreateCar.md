# 🧪 API Test Cases — Create Car & Upload Images

> **Project:** Car Rental API — C# / ASP.NET Core
> **Testing Tool:** Postman (Manual)
> **Total Cases:** 31 test cases

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

## 🗺️ Covered Endpoints

| Endpoint | Description |
|---|---|
| `POST /api/v1/cars` | Create a new car |
| `POST /api/v1/cars/{Id}/images` | Upload images for a car |

---

---

# Section 1: Create Car

**Endpoint:** `POST {{baseUrl}}/api/v1/cars`
**Content-Type:** `application/json`

---

## Summary

| # | Test Case | Type | Expected Status Code |
|---|-----------|------|----------------------|
| TC-01 | Create a car with all valid data | ✅ Happy Path | `201 Created` |
| TC-02 | Create a car with custom rates | ✅ Happy Path | `201 Created` |
| TC-03 | Branch does not exist | ❌ Error Case | `400 Bad Request` |
| TC-04 | Branch exists but is inactive | ❌ Error Case | `400 Bad Request` |
| TC-05 | Category does not exist or is inactive | ❌ Error Case | `400 Bad Request` |
| TC-06 | Duplicate PlateNumberEN | ❌ Error Case | `422 Unprocessable Entity` |
| TC-07 | Duplicate VIN | ❌ Error Case | `422 Unprocessable Entity` |
| TC-08 | PlateNumberEN wrong format | ❌ Error Case | `422 Unprocessable Entity` |
| TC-09 | VIN wrong length | ❌ Error Case | `422 Unprocessable Entity` |
| TC-10 | VIN contains forbidden characters (I/O/Q) | ❌ Error Case | `422 Unprocessable Entity` |
| TC-11 | Year out of allowed range | ❌ Error Case | `422 Unprocessable Entity` |
| TC-12 | FuelType out of allowed values | ❌ Error Case | `422 Unprocessable Entity` |
| TC-13 | NumberOfSeats out of range | ❌ Error Case | `422 Unprocessable Entity` |
| TC-14 | CustomDailyRate is negative | ❌ Error Case | `422 Unprocessable Entity` |
| TC-15 | Required field is empty (Brand) | ❌ Error Case | `422 Unprocessable Entity` |
| TC-16 | Completely empty request body | ⚠️ Edge Case | `422 Unprocessable Entity` |
| TC-17 | PlateNumberAR exceeds 20 characters | ⚠️ Edge Case | `422 Unprocessable Entity` |
| TC-18 | Brand exceeds 50 characters | ⚠️ Edge Case | `422 Unprocessable Entity` |
| TC-19 | Year = current year + 1 (maximum allowed) | ⚠️ Edge Case | `201 Created` |
| TC-20 | NumberOfBags = 0 (minimum allowed) | ⚠️ Edge Case | `201 Created` |
| TC-21 | Create car without Auth Cookie | 🔐 Auth Case | `401 Unauthorized` |
| TC-22 | Create car with expired Auth Cookie | 🔐 Auth Case | `401 Unauthorized` |
| TC-23 | Create car with valid Cookie but Customer role | 🔐 Auth Case | `403 Forbidden` |

---

## Test Case Details

---

### ✅ TC-01 — Create a car with all valid data

**Pre-conditions:**
- Branch with `Id=1` exists and `IsActive=true`
- Category with `Id=2` exists and `IsActive=true`
- Plate number `ABC 1234` is not already in use
- VIN `1HGBH41JXMN109186` is not already in use

**Request Body:**
```json
{
  "plateNumberEN": "ABC 1234",
  "plateNumberAR": "أ ب ج 1234",
  "vin": "1HGBH41JXMN109186",
  "brand": "Toyota",
  "model": "Camry",
  "year": 2023,
  "numberOfSeats": 5,
  "numberOfBags": 3,
  "engineCapacity": 2000,
  "fuelType": 1,
  "transmissionType": 0,
  "currentBranchId": 1,
  "categoryId": 2
}
```

**Expected Response — `201 Created`:**
```json
{
  "statusCode": 201,
  "succeeded": true,
  "message": "Created",
  "data": 15
}
```

> 💡 **`data`** holds the new car's `Id`. **Save this value** — you will need it in Section 2 to upload images.

---

### ✅ TC-02 — Create a car with custom rates

**Pre-conditions:** Same as TC-01 with a different plate and VIN.

**Request Body:**
```json
{
  "plateNumberEN": "XYZ 5678",
  "plateNumberAR": "س ط ع 5678",
  "vin": "2T1BURHE0JC043821",
  "brand": "Honda",
  "model": "Civic",
  "year": 2022,
  "numberOfSeats": 5,
  "numberOfBags": 2,
  "engineCapacity": 1500,
  "fuelType": 1,
  "transmissionType": 1,
  "currentBranchId": 1,
  "categoryId": 2,
  "customDailyRate": 150.00,
  "customWeeklyRate": 900.00,
  "customMonthlyRate": 3200.00
}
```

**Expected Response — `201 Created`:**
```json
{
  "statusCode": 201,
  "succeeded": true,
  "message": "Created",
  "data": 16
}
```

> 💡 `customDailyRate`, `customWeeklyRate`, and `customMonthlyRate` are all **nullable** — they can be omitted or included.

---

### ❌ TC-03 — Branch does not exist

**Pre-conditions:** No branch with `Id=9999` exists in the database.

**Request Body:**
```json
{
  "plateNumberEN": "TTT 1111",
  "plateNumberAR": "ت ت ت 1111",
  "vin": "3VWFE21C04M000001",
  "brand": "Nissan",
  "model": "Altima",
  "year": 2021,
  "numberOfSeats": 5,
  "numberOfBags": 2,
  "engineCapacity": 1800,
  "fuelType": 1,
  "transmissionType": 0,
  "currentBranchId": 9999,
  "categoryId": 2
}
```

**Expected Response — `400 Bad Request`:**
```json
{
  "statusCode": 400,
  "succeeded": false,
  "message": "Branch not found or is inactive.",
  "errors": ["Branch not found or is inactive."]
}
```

> 💡 This is a **Business Validation** check that runs inside the Handler after FluentValidation passes.

---

### ❌ TC-04 — Branch exists but is inactive

**Pre-conditions:** Branch with `Id=3` exists but `IsActive=false`.

**Request Body:**
```json
{
  "plateNumberEN": "DDD 2222",
  "plateNumberAR": "د د د 2222",
  "vin": "4T1BF3EK8AU561234",
  "brand": "Kia",
  "model": "Sportage",
  "year": 2020,
  "numberOfSeats": 5,
  "numberOfBags": 3,
  "engineCapacity": 2000,
  "fuelType": 1,
  "transmissionType": 0,
  "currentBranchId": 3,
  "categoryId": 2
}
```

**Expected Response — `400 Bad Request`:**
```json
{
  "statusCode": 400,
  "succeeded": false,
  "message": "Branch not found or is inactive.",
  "errors": ["Branch not found or is inactive."]
}
```

> 💡 Same error message as TC-03 — the API does not distinguish between "not found" and "inactive" in the response.

---

### ❌ TC-05 — Category does not exist or is inactive

**Pre-conditions:** Branch `Id=1` is valid — Category `Id=9999` does not exist.

**Request Body:**
```json
{
  "plateNumberEN": "CCC 3333",
  "plateNumberAR": "ج ج ج 3333",
  "vin": "5YFBURHE0HP123456",
  "brand": "Hyundai",
  "model": "Elantra",
  "year": 2022,
  "numberOfSeats": 5,
  "numberOfBags": 2,
  "engineCapacity": 1600,
  "fuelType": 1,
  "transmissionType": 0,
  "currentBranchId": 1,
  "categoryId": 9999
}
```

**Expected Response — `400 Bad Request`:**
```json
{
  "statusCode": 400,
  "succeeded": false,
  "message": "Category not found or is inactive.",
  "errors": ["Category not found or is inactive."]
}
```

---

### ❌ TC-06 — Duplicate PlateNumberEN

**Pre-conditions:** A car with `plateNumberEN='ABC 1234'` already exists in the database.

**Request Body:**
```json
{
  "plateNumberEN": "ABC 1234",
  "plateNumberAR": "م م م 9999",
  "vin": "WBAWB73589P123456",
  "brand": "BMW",
  "model": "320i",
  "year": 2023,
  "numberOfSeats": 5,
  "numberOfBags": 2,
  "engineCapacity": 2000,
  "fuelType": 1,
  "transmissionType": 1,
  "currentBranchId": 1,
  "categoryId": 2
}
```

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "message": "PlateNumberEN is already in use.",
  "errors": ["PlateNumberEN is already in use."]
}
```

> 💡 `422` is used for uniqueness conflicts, not for format errors.

---

### ❌ TC-07 — Duplicate VIN

**Pre-conditions:** A car with VIN `1HGBH41JXMN109186` already exists in the database.

**Request Body:**
```json
{
  "plateNumberEN": "NEW 0001",
  "plateNumberAR": "ج ج ج 0001",
  "vin": "1HGBH41JXMN109186",
  "brand": "Mercedes",
  "model": "C200",
  "year": 2023,
  "numberOfSeats": 5,
  "numberOfBags": 2,
  "engineCapacity": 2000,
  "fuelType": 1,
  "transmissionType": 1,
  "currentBranchId": 1,
  "categoryId": 2
}
```

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "message": "VIN is already in use.",
  "errors": ["VIN is already in use."]
}
```

---

### ❌ TC-08 — PlateNumberEN wrong format

**Required format:** `ABC 1234` — 3 uppercase English letters + space + 4 digits.

**Request Body:**
```json
{
  "plateNumberEN": "ab1234",
  "plateNumberAR": "أ ب ج 1234",
  "vin": "3N1CB51D01L123456",
  "brand": "Toyota",
  "model": "Yaris",
  "year": 2021,
  "numberOfSeats": 5,
  "numberOfBags": 1,
  "engineCapacity": 1300,
  "fuelType": 1,
  "transmissionType": 0,
  "currentBranchId": 1,
  "categoryId": 2
}
```

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "validationErrors": {
    "Dto.PlateNumberEN": "PlateNumberEN format must be 'ABC 1234'."
  }
}
```

> 💡 **FluentValidation** checks the Regex before the request reaches the Handler.

---

### ❌ TC-09 — VIN wrong length

**Request Body:**
```json
{
  "plateNumberEN": "FFF 4444",
  "plateNumberAR": "ف ف ف 4444",
  "vin": "SHORTVIN123",
  "brand": "Ford",
  "model": "Fusion",
  "year": 2020,
  "numberOfSeats": 5,
  "numberOfBags": 2,
  "engineCapacity": 1600,
  "fuelType": 1,
  "transmissionType": 0,
  "currentBranchId": 1,
  "categoryId": 2
}
```

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "validationErrors": {
    "Dto.VIN": "VIN must be exactly 17 characters."
  }
}
```

---

### ❌ TC-10 — VIN contains forbidden characters (I, O, Q)

**Reason:** The international VIN standard forbids I, O, and Q to avoid confusion with 1, 0, and numbers.

**Request Body:**
```json
{
  "plateNumberEN": "GGG 5555",
  "plateNumberAR": "غ غ غ 5555",
  "vin": "1HGBH41IXMN109186",
  "brand": "Chevrolet",
  "model": "Malibu",
  "year": 2021,
  "numberOfSeats": 5,
  "numberOfBags": 2,
  "engineCapacity": 1500,
  "fuelType": 1,
  "transmissionType": 0,
  "currentBranchId": 1,
  "categoryId": 2
}
```

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "validationErrors": {
    "Dto.VIN": "VIN contains invalid characters (I, O, Q are not allowed)."
  }
}
```

---

### ❌ TC-11 — Year out of allowed range

**Allowed range:** `1990` to `DateTime.UtcNow.Year + 1`

**Request Body:**
```json
{
  "plateNumberEN": "HHH 6666",
  "plateNumberAR": "ه ه ه 6666",
  "vin": "1G1BL52P7TR115252",
  "brand": "Audi",
  "model": "A4",
  "year": 1985,
  "numberOfSeats": 5,
  "numberOfBags": 2,
  "engineCapacity": 2000,
  "fuelType": 1,
  "transmissionType": 1,
  "currentBranchId": 1,
  "categoryId": 2
}
```

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "validationErrors": {
    "Dto.Year": "Year must be between 1990 and 2027."
  }
}
```

---

### ❌ TC-12 — FuelType out of allowed values

**Allowed values:** `1`=Petrol, `2`=Diesel, `3`=Electric, `4`=Hybrid

**Request Body:**
```json
{
  "plateNumberEN": "JJJ 7777",
  "plateNumberAR": "ج ج ج 7777",
  "vin": "2C3KA53G46H123456",
  "brand": "Volvo",
  "model": "XC60",
  "year": 2022,
  "numberOfSeats": 5,
  "numberOfBags": 3,
  "engineCapacity": 2000,
  "fuelType": 99,
  "transmissionType": 0,
  "currentBranchId": 1,
  "categoryId": 2
}
```

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "validationErrors": {
    "Dto.FuelType": "FuelType must be between 1 and 4."
  }
}
```

---

### ❌ TC-13 — NumberOfSeats out of range

**Allowed range:** `1` to `15`

**Request Body:**
```json
{
  "plateNumberEN": "KKK 8888",
  "plateNumberAR": "ك ك ك 8888",
  "vin": "1FMZU77K44UA12345",
  "brand": "BusCo",
  "model": "Mega",
  "year": 2022,
  "numberOfSeats": 50,
  "numberOfBags": 10,
  "engineCapacity": 3000,
  "fuelType": 2,
  "transmissionType": 0,
  "currentBranchId": 1,
  "categoryId": 2
}
```

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "validationErrors": {
    "Dto.NumberOfSeats": "NumberOfSeats must be between 1 and 15."
  }
}
```

---

### ❌ TC-14 — CustomDailyRate is negative

**Request Body:**
```json
{
  "plateNumberEN": "LLL 9999",
  "plateNumberAR": "ل ل ل 9999",
  "vin": "3VWSE69M63M123456",
  "brand": "Lexus",
  "model": "ES350",
  "year": 2023,
  "numberOfSeats": 5,
  "numberOfBags": 2,
  "engineCapacity": 3500,
  "fuelType": 1,
  "transmissionType": 1,
  "currentBranchId": 1,
  "categoryId": 2,
  "customDailyRate": -50.00
}
```

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "validationErrors": {
    "Dto.CustomDailyRate": "custom daily rate cannot be negative."
  }
}
```

> 💡 Validation is only triggered when the value is provided, due to `.When(x => x.CustomDailyRate.HasValue)`.

---

### ❌ TC-15 — Required field is empty (Brand)

**Request Body:**
```json
{
  "plateNumberEN": "MMM 1010",
  "plateNumberAR": "م م م 1010",
  "vin": "1N4AL3APXJC123456",
  "brand": "",
  "model": "Corolla",
  "year": 2022,
  "numberOfSeats": 5,
  "numberOfBags": 2,
  "engineCapacity": 1800,
  "fuelType": 1,
  "transmissionType": 0,
  "currentBranchId": 1,
  "categoryId": 2
}
```

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "validationErrors": {
    "Dto.Brand": "Property cannot be empty."
  }
}
```

---

---

# Edge Cases — Section 1

---

### ⚠️ TC-16 — Completely empty request body

**Description:** Send a POST request with no body at all.

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

> 💡 Handled by `CreateCarCommandValidator` via `.NotNull()` on the entire Dto.

---

### ⚠️ TC-17 — PlateNumberAR exceeds 20 characters

**Description:** The maximum allowed length for `PlateNumberAR` is 20 characters.

**Request Body:**
```json
{
  "plateNumberEN": "NNN 2020",
  "plateNumberAR": "أ ب ج د ه و ز ح ط ي ك ل م ن س ع ف ص 2020",
  "vin": "JN1AZ4EH2FM123456",
  "brand": "Toyota",
  "model": "Land Cruiser",
  "year": 2022,
  "numberOfSeats": 8,
  "numberOfBags": 5,
  "engineCapacity": 4000,
  "fuelType": 2,
  "transmissionType": 1,
  "currentBranchId": 1,
  "categoryId": 2
}
```

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "validationErrors": {
    "Dto.PlateNumberAR": "The length of 'Plate Number AR' must be 20 characters or fewer."
  }
}
```

---

### ⚠️ TC-18 — Brand exceeds 50 characters

**Description:** The maximum allowed length for `Brand` is 50 characters.

**Request Body:**
```json
{
  "plateNumberEN": "PPP 3030",
  "plateNumberAR": "ب ب ب 3030",
  "vin": "1G6KD57Y07U123456",
  "brand": "ThisIsAVeryLongBrandNameThatExceedsFiftyCharactersLimit",
  "model": "Sedan",
  "year": 2022,
  "numberOfSeats": 5,
  "numberOfBags": 2,
  "engineCapacity": 2000,
  "fuelType": 1,
  "transmissionType": 1,
  "currentBranchId": 1,
  "categoryId": 2
}
```

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "validationErrors": {
    "Dto.Brand": "The maximum length of Brand is 50."
  }
}
```

---

### ⚠️ TC-19 — Year = current year + 1 (maximum allowed boundary)

**Description:** Test the upper boundary of the allowed year — should be accepted.

**Request Body:**
```json
{
  "plateNumberEN": "QQQ 4040",
  "plateNumberAR": "ق ق ق 4040",
  "vin": "3FADP4BJ5EM123456",
  "brand": "Tesla",
  "model": "Model 3",
  "year": 2027,
  "numberOfSeats": 5,
  "numberOfBags": 2,
  "engineCapacity": 0,
  "fuelType": 3,
  "transmissionType": 1,
  "currentBranchId": 1,
  "categoryId": 2
}
```

**Expected Response — `201 Created`:**
```json
{
  "statusCode": 201,
  "succeeded": true,
  "message": "Created",
  "data": 17
}
```

> 💡 `year=2027` is valid as of 2026 (current year + 1). `engineCapacity=0` is allowed for electric vehicles.

---

### ⚠️ TC-20 — NumberOfBags = 0 (minimum allowed boundary)

**Description:** The value `0` is allowed for `NumberOfBags` — should be accepted.

**Request Body:**
```json
{
  "plateNumberEN": "RRR 5050",
  "plateNumberAR": "ر ر ر 5050",
  "vin": "1FADP3F2XEL123456",
  "brand": "Smart",
  "model": "ForTwo",
  "year": 2022,
  "numberOfSeats": 2,
  "numberOfBags": 0,
  "engineCapacity": 900,
  "fuelType": 1,
  "transmissionType": 1,
  "currentBranchId": 1,
  "categoryId": 2
}
```

**Expected Response — `201 Created`:**
```json
{
  "statusCode": 201,
  "succeeded": true,
  "message": "Created",
  "data": 18
}
```

---

---

# Auth Cases — Section 1

> 🔐 **TODO:** These cases are currently disabled because `[Authorize(Roles = Roles.Admin)]` is commented out in `AdminCarsController`.
> Enable and run them after removing the comment and activating the Auth Middleware.

---

### 📖 How Authentication Works in This Project

The project uses **httpOnly Cookies** — not `Authorization: Bearer` headers.

**Key difference:**

| Method | How the Token is sent | Readable by JavaScript? |
|--------|-----------------------|-------------------------|
| `Authorization: Bearer` | Manually in each request header | ✅ Yes |
| **httpOnly Cookie** | **Browser/Postman sends it automatically** | ❌ No — protected from XSS |

**Why httpOnly Cookie is more secure:**
The Token is stored in a Cookie that no JavaScript on the page can read or steal, protecting against **XSS (Cross-Site Scripting)** attacks.

**Authentication flow in this project:**
```
1. User sends credentials  -->  POST /api/v1/authentication/signin
2. Server returns a Cookie containing the JWT (Set-Cookie header)
3. Postman stores it automatically and sends it with every subsequent request
4. Server reads the Token from the Cookie and verifies the Role
```

**In Postman:** Enable **"Automatically follow redirects"** and **"Save cookies"** so Postman stores the Cookie after sign-in and sends it automatically with requests.

---

### 🔐 TC-21 — Create car without Auth Cookie (not signed in)

> **TODO:** Enable after removing the comment from `[Authorize]`.

**Description:** Request arrives with no Cookie — the user has not performed Sign In.

**Postman Setup:**
- Make sure no Cookie is stored for the domain in Postman
- To clear: click **Cookies** (below the Send button) → find the domain → delete all Cookies

**Request Body:** *(same as TC-01)*

**Expected Response — `401 Unauthorized`:**
```json
{
  "statusCode": 401,
  "succeeded": false,
  "message": "Unauthorized",
  "errors": ["Unauthorized"]
}
```

> 💡 The server finds no Cookie in the request → no Token to verify → request is rejected immediately.

---

### 🔐 TC-22 — Create car with expired Auth Cookie

> **TODO:** Enable after removing the comment from `[Authorize]`.

**Description:** Cookie is present but the JWT inside it has expired.

**How to test it:**
1. Sign in and wait until the Token expires (based on `TokenValidityInMinutes` in the project config)
2. Send the request immediately after expiry

**Expected Response — `401 Unauthorized`:**
```json
{
  "statusCode": 401,
  "succeeded": false,
  "message": "Unauthorized",
  "errors": ["Unauthorized"]
}
```

> 💡 The server finds the Cookie and reads the Token, but discovers that `exp` (expiry) in the JWT payload has passed → request is rejected.

---

### 🔐 TC-23 — Create car with valid Cookie but Customer role

> **TODO:** Enable after removing the comment from `[Authorize(Roles = Roles.Admin)]`.

**Description:** Cookie is valid and Token is not expired, but the user's role is `Customer` not `Admin`.

**How to test it:**
1. Sign in with a regular Customer account → `POST /api/v1/authentication/signin`
2. Send the create car request — Postman will send the Cookie automatically

**Expected Response — `403 Forbidden`:**
```json
{
  "statusCode": 403,
  "succeeded": false,
  "message": "Forbidden",
  "errors": ["Forbidden"]
}
```

> 💡 Difference between 401 and 403: **401** = no Token or invalid Token. **403** = valid Token but insufficient permissions.

---

---

# Section 2: Upload Car Images

**Endpoint:** `POST {{baseUrl}}/api/v1/cars/{Id}/images`
**Content-Type:** `multipart/form-data`

> ⚠️ **Important:** You must first run TC-01 or TC-02 and obtain the car `Id` to use in this section.

---

## Summary

| # | Test Case | Type | Expected Status Code |
|---|-----------|------|----------------------|
| TC-24 | Upload a single valid JPEG image | ✅ Happy Path | `201 Created` |
| TC-25 | Upload multiple images in one request (JPEG+PNG+WebP) | ✅ Happy Path | `201 Created` |
| TC-26 | Car does not exist | ❌ Error Case | `404 Not Found` |
| TC-27 | More than 10 images in one request | ❌ Error Case | `422 Unprocessable Entity` |
| TC-28 | File exceeds 5 MB | ❌ Error Case | `422 Unprocessable Entity` |
| TC-29 | Unsupported file type (PDF) | ❌ Error Case | `422 Unprocessable Entity` |
| TC-30 | Spoofed Magic Bytes (text file renamed to .jpg) | ❌ Error Case | `422 Unprocessable Entity` |
| TC-31 | Request sent with no files | ❌ Error Case | `422 Unprocessable Entity` |

---

## Test Case Details

---

### ✅ TC-24 — Upload a single valid JPEG image

**Pre-conditions:**
- Car with `Id=15` exists (created in TC-01)
- No images exist yet for this car
- A real `.jpg` image smaller than 5 MB is available on your machine

**Postman Setup:**
```
Method : POST
URL    : {{baseUrl}}/api/v1/cars/15/images
Body   : form-data
  Key  : files    Type: File    Value: [select image from your machine]
```

> 💡 **How to select a file in Postman:**
> In the Value field — click the small dropdown next to the field and choose **File** instead of **Text**, then click **Select Files** and pick the image.

**Expected Response — `201 Created`:**
```json
{
  "statusCode": 201,
  "succeeded": true,
  "message": "Created",
  "data": [1]
}
```

> 💡 `data` is a `List<int>` containing the saved image's `Id`. The first image uploaded for any car automatically becomes the **Primary** image.
> All uploaded images are converted to **WebP** before saving, regardless of the original format.
> Actual save path: `storage/cars/{carId}/car_{carId}_{guid}.webp`

---

### ✅ TC-25 — Upload multiple images in one request

**Pre-conditions:** Car with `Id=15` exists — multiple image files of different types are available.

**Postman Setup:**
```
Method : POST
URL    : {{baseUrl}}/api/v1/cars/15/images
Body   : form-data
  Key  : files    Type: File    Value: photo1.jpg
  Key  : files    Type: File    Value: photo2.png
  Key  : files    Type: File    Value: photo3.webp
```

> 💡 **Important:** The Key must be exactly **`files`** in all rows — the same name for every file.

**Expected Response — `201 Created`:**
```json
{
  "statusCode": 201,
  "succeeded": true,
  "message": "Created",
  "data": [2, 3, 4]
}
```

---

### ❌ TC-26 — Car does not exist

**Postman Setup:**
```
Method : POST
URL    : {{baseUrl}}/api/v1/cars/9999/images
Body   : form-data
  Key  : files    Type: File    Value: [any valid image]
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

### ❌ TC-27 — More than 10 images in one request

**Postman Setup:**
```
Method : POST
URL    : {{baseUrl}}/api/v1/cars/15/images
Body   : form-data
  Key  : files    Type: File    Value: img1.jpg
  Key  : files    Type: File    Value: img2.jpg
  ... (add 11 rows with the same Key)
  Key  : files    Type: File    Value: img11.jpg
```

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "errors": ["Cannot upload more than 10 images at once."]
}
```

> 💡 The limit of 10 applies per request — it is not a limit on the total number of images a car can have.

---

### ❌ TC-28 — File exceeds 5 MB

**Pre-conditions:** An image larger than 5,242,880 bytes (5 MB) is available.

**Postman Setup:**
```
Method : POST
URL    : {{baseUrl}}/api/v1/cars/15/images
Body   : form-data
  Key  : files    Type: File    Value: large_photo.jpg  (> 5 MB)
```

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "errors": ["Each file must not exceed 5 MB."]
}
```

---

### ❌ TC-29 — Unsupported file type (PDF)

**Description:** Only JPEG, PNG, and WebP are accepted. Any other type must be rejected.

**Postman Setup:**
```
Method : POST
URL    : {{baseUrl}}/api/v1/cars/15/images
Body   : form-data
  Key  : files    Type: File    Value: document.pdf
```

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "errors": ["Only JPEG, PNG, or WebP images are allowed."]
}
```

---

### ❌ TC-30 — Spoofed Magic Bytes (text file renamed to .jpg)

**Description:** The API reads the first 12 bytes of every file to verify its real type — renaming the extension is not enough.

**How to create the test file:**

**Windows:**
1. Open Notepad and type any random text
2. Save it as `test.txt`
3. Rename it to `fake_image.jpg`
4. Click Yes when Windows asks to confirm the extension change
5. Upload this file — the API will reject it

**macOS:**
```bash
echo "this is not an image" > ~/Desktop/fake_image.jpg
```

**Postman Setup:**
```
Method : POST
URL    : {{baseUrl}}/api/v1/cars/15/images
Body   : form-data
  Key  : files    Type: File    Value: fake_image.jpg  (text file with spoofed extension)
```

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "errors": ["File content does not match the declared image type."]
}
```

> 💡 **Magic Bytes:** Every image type has a fixed signature in its first bytes:
> - JPEG: `FF D8 FF`
> - PNG: `89 50 4E 47`
> - WebP: `52 49 46 46 ... 57 45 42 50`

---

### ❌ TC-31 — Request sent with no files

**Postman Setup:**
```
Method : POST
URL    : {{baseUrl}}/api/v1/cars/15/images
Body   : form-data  (empty — do not add any file)
```

**Expected Response — `422 Unprocessable Entity`:**
```json
{
  "statusCode": 422,
  "succeeded": false,
  "errors": ["At least one image file is required."]
}
```

---

---

## 📋 Quick Reference — Allowed Values

### FuelType

| Value | Name |
|-------|------|
| `1` | Petrol |
| `2` | Diesel |
| `3` | Electric |
| `4` | Hybrid |

### TransmissionType

| Value | Name |
|-------|------|
| `1` | Manual |
| `2` | Automatic |

### Field Constraints

| Field | Min | Max |
|-------|-----|-----|
| `year` | `1990` | `current year + 1` |
| `numberOfSeats` | `1` | `15` |
| `numberOfBags` | `0` | `10` |
| `engineCapacity` | `0` | — |
| `plateNumberAR` | — | `20` characters |
| `brand` | — | `50` characters |
| `model` | — | `50` characters |
| `vin` | exactly `17` characters | exactly `17` characters |
| Image file size | — | `5 MB` |
| Images per request | `1` | `10` |