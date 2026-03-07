# Get Admin Car By Id

## Description
Fetch details of a single car for the Admin Panel.  
The response uses the standard Response<T> envelope from ResponseHandler.  
Includes Activity Log entries for auditing purposes.

## Request

| Parameter | Type | Required | Description            |
|-----------|------|----------|------------------------|
| id        | int  | Yes      | The unique Car ID      |

Example Request:
GET /api/admin/cars/123 HTTP/1.1
Host: example.com
Authorization: Bearer {admin_token}

## Successful Response (200 OK)
```
{
  "statusCode": 200,
  "succeeded": true,
  "message": "Success",
  "errors": [],
  "meta": null,
  "validationErrors": null,
  "data": {
    "id": 123,
    "plateNumberEN": "ABC123",
    "plateNumberAR": "أ ب ج ١٢٣",
    "vin": "1HGCM82633A004352",
    "brand": "Toyota",
    "model": "Corolla",
    "year": 2022,
    "fuelType": "Petrol",
    "transmissionType": "Automatic",
    "category": { "id": 2, "name": "Standard" },
    "currentBranch": { "id": 5, "name": "Riyadh Branch" },
    "isActive": true,
    "fleetConditionStatus": "Ready",
    "dailyRate": 100.0,
    "weeklyRate": 600.0,
    "monthlyRate": 2200.0,
    "createdAt": "2026-03-07T10:00:00Z",
    "images": [
      "/admin/cars/123/images/1",
      "/admin/cars/123/images/2"
    ]
  }
}
```


## Error Responses

### Not Found (404):

```
{
  "statusCode": 404,
  "succeeded": false,
  "message": "Car not found.",
  "errors": ["Car not found."],
  "meta": null,
  "validationErrors": null,
  "data": null
}
```

### Unauthorized (401):

```
{
  "statusCode": 401,
  "succeeded": false,
  "message": "Unauthorized",
  "errors": ["Unauthorized"],
  "meta": null,
  "validationErrors": null,
  "data": null
}
```

### Internal Server Error (500):

```
{
  "statusCode": 500,
  "succeeded": false,
  "message": "Internal Server Error",
  "errors": ["Internal Server Error"],
  "meta": null,
  "validationErrors": null,
  "data": null
}
```

## Activity Log

Every request logs:

| Field       | Description                               |
|-------------|-------------------------------------------|
| Action      | GetAdminCarById                           |
| CarId       | The requested Car ID                      |
| AdminUserId | ID of the admin performing the request    |
| Timestamp   | UTC datetime of the request               |
| Status      | Success / NotFound / Error                |
| Error       | Exception message if Status = Error       |

Example Log Entries:
[2026-03-07 10:05:12 UTC] Action=GetAdminCarById, CarId=123, AdminUserId=42, Status=Success
[2026-03-07 10:06:30 UTC] Action=GetAdminCarById, CarId=999, AdminUserId=42, Status=NotFound
[2026-03-07 10:07:55 UTC] Action=GetAdminCarById, CarId=123, AdminUserId=42, Status=Error, Error="SQL Timeout"

## Test Cases (Manual / Postman)
| Test Case               | Request                             | Expected Response         |
|-------------------------|-------------------------------------|---------------------------|
| Valid Car               | id=123                              | 200 OK, data filled       |
| Non-existent Car        | id=999                              | 404 Not Found             |
| Unauthorized            | Non Admin Token/Missing token       | 401 Unauthorized          |
| Internal Error          | Simulate DB down                    | 500 Internal Server Error |
| Car without images      | id=124                              | 200 OK, images=[]         |
| Car With Multiple Images| All Car Images & non-deleted images | 200 OK                    |
| Car with deleted images | Only primary & non-deleted images   | 200 OK                    |

Notes:
- Token must belong to an admin user.
- FuelType and TransmissionType use enum display names.
- Only active images (!IsDeleted) are returned.
- FleetConditionStatus uses ToDisplayName() extension.
- All requests are logged in Activity Log with AdminUserId and CarId.
*/

