HoraLife Store Checkout Preview Postman Collection
=================================================

Import
------
Import:
HoraLife_Store_Checkout_Preview.postman_collection.json

Use your existing HoraLife Local environment with:
- baseUrl
- accessToken

Store the JWT in accessToken without the 'Bearer ' prefix.

Prerequisites
-------------
1. Authenticated user has a non-empty active cart.
2. Authenticated user has at least one Shipping address.
3. A Billing address is required only for the separate-Billing request.

Recommended run order
---------------------
1. Precheck - Get Active Cart
2. Precheck - Get Addresses and Capture IDs
3. Checkout Preview - Shipping as Billing
4. Checkout Preview - Separate Billing Address
5. Invalid Shipping Address - 404
6. Invalid Billing Address - 404
7. Unauthorized Checkout Preview - 401

The address precheck automatically captures:
- shippingAddressId
- billingAddressId

Move into main collection
-------------------------
After importing, move the Checkout Preview folder into:

HoraLife API
└── Store
    └── Checkout Preview

Then delete the temporary standalone collection if desired.
