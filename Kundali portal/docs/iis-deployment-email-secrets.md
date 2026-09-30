# IIS Deployment And SMTP Password Encryption

## Publish Backend

From `D:\Learning\Ai Kundali\GIT\Kundali portal\KundaliEngine`:

```powershell
dotnet publish KundaliEngine.API\KundaliEngine.API.csproj -c Release -o .\publish\api
```

Copy `.\publish\api` to the VM, for example `C:\inetpub\kundali-api`.

## IIS Setup

1. Install the .NET 8 Hosting Bundle on the VM.
2. Create an IIS site pointing to `C:\inetpub\kundali-api`.
3. Use an application pool with `No Managed Code`.
4. Set `ASPNETCORE_ENVIRONMENT=Production` for the site.
5. Configure database, JWT, CORS, and SMTP settings in `appsettings.Production.json` or IIS environment variables.

## Publish Frontend

From `Frontend\kundali-portal-ui`:

```powershell
npm run build -- --configuration production
```

On the VM, **empty the IIS UI folder first**, then copy `dist\kundali-portal-ui\browser\*` (or the folder Angular printed as Output location). Do not copy the new `dist` on top of old hashed `.js` files.

Use a production build (`outputHashing: all`). A default `ng build` (development) writes unhashed `main.js`, which IIS and the browser keep serving after you deploy.

`web.config` in the dist root tells IIS not to cache `index.html` or `i18n/*.json`. Hashed JS/CSS can stay cached.

After a deploy, one hard refresh (`Ctrl+F5`) is enough for that browser. Mixed old/new screens usually mean an old `index.html` or old `en.json` was still cached.

## Encrypt SMTP Password

Set a strong key on the VM. Keep this outside appsettings.

```powershell
[Environment]::SetEnvironmentVariable('KUNDALI_SMTP_PASSWORD_KEY', '<strong-random-key>', 'Machine')
```

Restart IIS or the app pool after setting the key.

Generate the encrypted password using the same key:

```powershell
$env:KUNDALI_SMTP_PASSWORD_KEY = '<strong-random-key>'
.\tools\protect-smtp-password.ps1 -Password '<smtp-password>'
```

Put the output into production config:

```json
{
  "ReportShare": {
    "Smtp": {
      "UserName": "yourhora@horalife.in",
      "Password": "",
      "EncryptedPassword": "v1:...",
      "PasswordKeyEnvironmentVariable": "KUNDALI_SMTP_PASSWORD_KEY"
    }
  }
}
```

At runtime, the API decrypts `EncryptedPassword` only when sending email. If `EncryptedPassword` is empty, it falls back to `Password` for local development.