# Kundali Portal Frontend - Architecture Documentation

## Overview

Professional, scalable Angular 21+ frontend for the Kundali Portal astrology platform. Built with standalone components, clean separation of concerns, and enterprise-grade structure.

## Project Structure

```
kundali-portal-ui/
├── src/
│   ├── app/
│   │   ├── core/                          # Core functionality (singletons)
│   │   │   ├── services/
│   │   │   │   ├── base-api.service.ts                # Base HTTP service with error handling
│   │   │   │   ├── kundali-api.service.ts           # Kundali-specific API endpoints
│   │   │   │   ├── compatibility-api.service.ts     # Compatibility analysis endpoints
│   │   │   │   ├── panchang-api.service.ts          # Panchang/calendar endpoints
│   │   │   │   └── dasha-api.service.ts             # Dasha period endpoints
│   │   │   ├── interceptors/               # HTTP interceptors (auth, error handling)
│   │   │   └── guards/                     # Route guards (auth, role-based)
│   │   │
│   │   ├── shared/                         # Reusable across modules
│   │   │   ├── models/
│   │   │   │   └── api.models.ts          # Shared DTOs and interfaces
│   │   │   ├── components/
│   │   │   │   ├── loading-spinner.component.ts    # Reusable loader
│   │   │   │   ├── section-card.component.ts       # Card container
│   │   │   │   └── page-header.component.ts        # Page title & breadcrumbs
│   │   │   └── utils/
│   │   │       └── common.utils.ts        # Shared utility functions
│   │   │
│   │   ├── layout/                         # Main application shell
│   │   │   ├── layout.component.ts        # Main layout wrapper
│   │   │   ├── navbar.component.ts        # Top navigation bar
│   │   │   └── sidebar.component.ts       # Left sidebar navigation
│   │   │
│   │   ├── modules/                        # Feature modules (lazy loaded)
│   │   │   ├── dashboard/
│   │   │   │   └── dashboard.component.ts
│   │   │   ├── kundali/
│   │   │   │   └── kundali.component.ts
│   │   │   ├── compatibility/
│   │   │   │   └── compatibility.component.ts
│   │   │   ├── panchang/
│   │   │   │   └── panchang.component.ts
│   │   │   └── dasha/
│   │   │       └── dasha.component.ts
│   │   │
│   │   ├── config/                         # Configuration files
│   │   │   ├── theme/
│   │   │   │   ├── theme.config.ts        # Theme variables (TS object)
│   │   │   │   └── theme.scss             # SCSS variables & mixins
│   │   │   └── environment/
│   │   │       ├── environment.ts         # Development config
│   │   │       ├── environment.prod.ts    # Production config
│   │   │       └── environment.staging.ts # Staging config
│   │   │
│   │   ├── app.routes.ts                  # Main routing configuration
│   │   ├── app.config.ts                  # Application providers
│   │   ├── app.ts                         # Root component
│   │   ├── app.html                       # Root template
│   │   └── app.scss                       # App-level styles
│   │
│   ├── styles.scss                         # Global styles & theme imports
│   ├── main.ts                             # Bootstrap application
│   └── index.html                          # HTML entry point
│
├── angular.json                            # Angular CLI configuration
├── tsconfig.json                           # TypeScript configuration
└── package.json                            # Dependencies
```

## Architecture Layers

### 1. **Core Layer** (`/app/core`)
Singleton services and infrastructure.

- **Services**: API communication, authentication, state management
- **Interceptors**: HTTP interceptors for auth tokens, error handling
- **Guards**: Route protection, permission checking

**When to add**: Once per service, used across the app

### 2. **Shared Layer** (`/app/shared`)
Reusable components and utilities.

- **Components**: Generic UI elements (spinner, cards, headers)
- **Models**: Shared interfaces and DTOs
- **Utils**: Helper functions (date formatting, validation, etc.)

**When to add**: Used in multiple modules

### 3. **Layout Layer** (`/app/layout`)
Application shell and navigation.

- **LayoutComponent**: Main container with router outlet
- **NavbarComponent**: Top navigation with quick links
- **SidebarComponent**: Left navigation with modules

**Usage**: Wraps all routed components

### 4. **Module Layer** (`/app/modules`)
Feature modules representing major functionality.

Each module should contain:
- Feature component(s)
- Module-specific services (optional)
- Module routing
- Feature-specific utilities

**Structure**:
```
modules/
├── dashboard/
│   ├── dashboard.component.ts
│   ├── dashboard.component.html
│   └── dashboard.component.scss
└── [other-modules]/
```

### 5. **Config Layer** (`/app/config`)
Application configuration and theming.

- **Theme**: Colors, typography, spacing, breakpoints
- **Environment**: API URLs, feature flags per environment

---

## Key Design Decisions

### 1. **Standalone Components**
All components are standalone (no NgModule required).

```typescript
@Component({
  selector: 'app-example',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `...`,
  styles: [`...`]
})
export class ExampleComponent {}
```

### 2. **Separation of Concerns**
- **API Layer**: BaseApiService → Specific service (KundaliApiService)
- **Components Layer**: Dumb components (input/output)
- **Services Layer**: Business logic, HTTP calls
- **Utils Layer**: Pure functions

### 3. **Reusable Components**
Common UI patterns extracted as standalone components:
- `LoadingSpinnerComponent` - Loading states
- `SectionCardComponent` - Content containers
- `PageHeaderComponent` - Consistent page headers

### 4. **Centralized Theme**
- SCSS variables for colors, spacing, typography
- TypeScript config object for programmatic access
- Mixins for common patterns (flexbox, transitions, etc.)

### 5. **Environment-Based Configuration**
Three environments: `development`, `staging`, `production`

```typescript
// src/app/config/environment/environment.ts
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5000/api',
  features: { /* ... */ }
};
```

---

## Routing Architecture

**File**: `src/app/app.routes.ts`

```typescript
routes: [
  {
    path: '',
    component: LayoutComponent,  // Main layout shell
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'kundali', component: KundaliComponent },
      { path: 'compatibility', component: CompatibilityComponent },
      { path: 'panchang', component: PanchangComponent },
      { path: 'dasha', component: DashaComponent },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
]
```

**Key Points**:
- All routes render within `LayoutComponent`
- Navbar and sidebar always visible
- Content area changes based on route
- Default route redirects to dashboard

---

## API Service Pattern

### Base Service (Core)
```typescript
// core/services/base-api.service.ts
@Injectable({ providedIn: 'root' })
export class BaseApiService {
  protected get<T>(endpoint: string): Observable<ApiResponse<T>> { ... }
  protected post<T>(endpoint: string, body: any): Observable<ApiResponse<T>> { ... }
  protected handleError(error: any) { ... }
}
```

### Feature Service (extends Base)
```typescript
// core/services/kundali-api.service.ts
@Injectable({ providedIn: 'root' })
export class KundaliApiService extends BaseApiService {
  getKundaliCharts(): Observable<ApiResponse<Kundali[]>> {
    return this.get<Kundali[]>('kundali');
  }
}
```

**Benefits**:
- Centralized error handling
- Consistent response format
- Timeout handling
- Easy to test and maintain

---

## Theme System

### SCSS Approach
**File**: `src/app/config/theme/theme.scss`

```scss
// Colors
$color-primary: #7E3BA8;
$color-secondary: #FF1E64;

// Typography
$font-family-base: 'Inter', sans-serif;
$font-size-base: 1rem;

// Spacing
$spacing-md: 1rem;

// Mixins
@mixin card-base {
  background: $color-white;
  border-radius: 1rem;
  box-shadow: $shadow-md;
  padding: $spacing-lg;
}
```

### TypeScript Approach
**File**: `src/app/config/theme/theme.config.ts`

For programmatic access (React-like):
```typescript
export const themeConfig = {
  colors: { /* ... */ },
  typography: { /* ... */ }
};
```

---

## Component Hierarchy

```
LayoutComponent (shell)
├── NavbarComponent (sticky top)
├── SidebarComponent (left, responsive)
└── Content Area (router-outlet)
    ├── DashboardComponent
    ├── KundaliComponent
    ├── CompatibilityComponent
    ├── PanchangComponent
    └── DashaComponent
```

---

## Shared Components Usage

### LoadingSpinnerComponent
```typescript
<app-loading-spinner 
  [size]="'60px'" 
  [message]="'Loading...'"
  [fullScreen]="false">
</app-loading-spinner>
```

### SectionCardComponent
```typescript
<app-section-card 
  title="Card Title" 
  [interactive]="true" 
  [highlight]="true">
  <!-- Your content here -->
</app-section-card>
```

### PageHeaderComponent
```typescript
<app-page-header
  title="Page Title"
  subtitle="Optional subtitle"
  description="Page description"
  [breadcrumbs]="[
    { label: 'Home', routerLink: '/' },
    { label: 'Current Page', active: true }
  ]"
  [showActions]="true"
></app-page-header>
```

---

## Development Workflow

### Adding a New Feature Module

1. **Create module structure**:
```bash
src/app/modules/[feature-name]/
├── [feature].component.ts
├── [feature].component.html
└── [feature].component.scss
```

2. **Create component**:
```typescript
@Component({
  selector: 'app-[feature]',
  standalone: true,
  imports: [CommonModule, PageHeaderComponent, SectionCardComponent],
  template: `...`,
  styles: [`...`]
})
export class [Feature]Component {}
```

3. **Add route** in `app.routes.ts`:
```typescript
{
  path: '[feature]',
  component: [Feature]Component,
  data: { title: 'Feature Title' }
}
```

4. **Add navigation** in sidebar/navbar

### Adding a New Service

1. **Create service** in `core/services/`:
```typescript
@Injectable({ providedIn: 'root' })
export class [Feature]ApiService extends BaseApiService {
  getFrom[Feature](): Observable<ApiResponse<Data>> {
    return this.get<Data>('[feature-endpoint]');
  }
}
```

2. **Use in component**:
```typescript
constructor(private service: [Feature]ApiService) {}

ngOnInit() {
  this.service.getFrom[Feature]().subscribe(
    (response) => { /* handle data */ },
    (error) => { /* handle error */ }
  );
}
```

---

## Responsive Design

### Breakpoints (from `theme.scss`)
- `xs`: < 460px (mobile)
- `sm`: 640px (tablet small)
- `md`: 768px (tablet)
- `lg`: 1024px (desktop)
- `xl`: 1280px (large desktop)
- `2xl`: 1536px (extra large)

### Mixin Usage
```scss
@include respond-to('md') {
  .grid {
    grid-template-columns: repeat(2, 1fr);
  }
}
```

---

## Performance Optimization

### Future Enhancements
1. **Lazy Loading**: Module-level route lazy loading
2. **Cache**: HTTP cache interceptor for GET requests
3. **State Management**: NgRx for complex state
4. **Code Splitting**: Dynamic imports for large modules
5. **Change Detection**: OnPush strategy for components
6. **Bundle Analysis**: Webpack Bundle Analyzer

---

## Security Best Practices

1. **HTTP Interceptors**: Add auth tokens to requests
2. **Route Guards**: Protect routes with authentication
3. **CORS**: Backend should handle CORS properly
4. **CSP Headers**: Set Content Security Policy
5. **Input Sanitization**: Use Angular's built-in sanitization

---

## Testing Structure

### Unit Tests
```typescript
describe('KundaliApiService', () => {
  it('should fetch kundali charts', () => { /* ... */ });
});
```

**Run**: `npm test`

### E2E Tests
```typescript
describe('Dashboard Page', () => {
  it('should display dashboard', () => { /* ... */ });
});
```

---

## Deployment

### Build for Production
```bash
npm run build
```

Output: `dist/kundali-portal-ui/`

### Serve Static Files
Configure your web server to:
1. Serve `dist/kundali-portal-ui/` as root
2. Redirect all routes to `index.html` (for SPA routing)

---

## Future Extensibility

### Ready for:
- ✅ Charts (integration with chart libraries)
- ✅ AI Summaries (service for AI endpoints)
- ✅ Reports (PDF generation)
- ✅ Multiple dashboards (admin, customer, astrologer)
- ✅ Real-time updates (WebSocket integration)
- ✅ Internationalization (i18n)
- ✅ Dark mode (CSS variables)
- ✅ Progressive Web App (PWA support)

---

## Quick Reference

### Common Commands
```bash
# Start dev server
npm start

# Build for production
npm run build

# Run tests
npm test

# Serve production build locally
npm run build && npx http-server dist/kundali-portal-ui -p 4200
```

### File Locations
- Components: `/app/layout/` or `/app/modules/[feature]/`
- Services: `/app/core/services/`
- Models: `/app/shared/models/`
- Utilities: `/app/shared/utils/`
- Styles: `/app/config/theme/`
- Routes: `/app/app.routes.ts`

---

## Support & Maintenance

- **Component Updates**: Update in place, imports handle dependencies
- **API Changes**: Update service, UI auto-updates via Observable
- **Theme Changes**: Update `theme.scss` and `theme.config.ts`
- **Resolution**: Check `core/interceptors/` for HTTP issues

---

**Last Updated**: May 2026  
**Angular Version**: 21.2.0  
**TypeScript Version**: 5.9.2
