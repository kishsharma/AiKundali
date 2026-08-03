HoraLife Auth Login Postman Collection

1. Import the JSON collection.
2. Select your existing Postman environment with:
   baseUrl = https://localhost:7245
3. Open collection Variables and set:
   authLoginPath = /api/auth/login
   loginIdentifierField = username
   loginIdentifier = demo
   loginPassword = your real password
4. Run: 1. Login and Save JWT
5. The script saves the token into environment variable:
   accessToken
6. Run: 2. Verify JWT - Get Store Cart

If your login DTO uses email, set:
loginIdentifierField = email

If Swagger shows a different route, change authLoginPath.

Do not export or share the collection after storing a real password.
