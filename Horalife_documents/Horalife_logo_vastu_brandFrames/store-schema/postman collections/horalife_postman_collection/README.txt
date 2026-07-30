HoraLife Postman Setup
=======================

1. Open Postman and click Import.
2. Import both JSON files.
3. Select the HoraLife Local environment.
4. Update baseUrl to the URL shown when KundaliEngine.Api starts.
5. Run Store > Products > Get Products first; it saves productSlug automatically.
6. Run Get Product by Slug.
7. Run the complete Store folder with Collection Runner.

For a local self-signed HTTPS certificate error, temporarily disable SSL certificate verification in Postman Settings > General. Re-enable it for hosted environments.

The negative tests intentionally expect 400 and 404 responses.
