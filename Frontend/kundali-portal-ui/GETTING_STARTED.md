# Kundali Portal Frontend - Setup & Getting Started Guide

## Prerequisites

- **Node.js**: v18+ (includes npm 10+)
- **Angular CLI**: v21.2.1+ (`npm install -g @angular/cli`)
- **Code Editor**: VS Code recommended
- **Backend API**: Running .NET API on `http://localhost:5000`

---

## Quick Start

### 1. Install Dependencies
```bash
npm install
```

### 2. Start Development Server
```bash
npm start
```

Server will run at: **http://localhost:4200**

### 3. Access Application
- **Dashboard**: http://localhost:4200
- **Kundali**: http://localhost:4200/kundali
- **Compatibility**: http://localhost:4200/compatibility
- **Panchang**: http://localhost:4200/panchang
- **Dasha**: http://localhost:4200/dasha

---

## Project Setup Walkthrough

### Directory Review
```bash
# View folder structure
tree src/app -L 2
```

### Key Folders Explained

| Folder | Purpose |
|--------|---------|
| `src/app/core/services/` | API communication layer |
| `src/app/shared/components/` | Reusable UI components |
| `src/app/shared/models/` | Data interfaces (DTOs) |
| `src/app/layout/` | Navigation & page shell |
| `src/app/modules/` | Feature pages |
| `src/app/config/` | Theme & environment config |

---

## Configuration

### Environment Setup

Edit `src/app/config/environment/environment.ts`:

```typescript
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5000/api',  // ← Your backend URL
  apiTimeout: 30000,
  features: {
    dashboard: true,
    kundali: true,
    compatibility: true,
    panchang: true,
    dasha: true,
  },
};
```

**For different environments**:
- Development: `environment.ts`
- Production: `environment.prod.ts`
- Staging: `environment.staging.ts`

### Build Configurations

The app automatically uses the correct environment during build:

```bash
# Development (default)
npm start

# Production
ng build --configuration production

# Staging
ng build --configuration staging
```

---

## Creating Your First Component

### Step-by-Step Example: Add a "Reports" Page

#### 1. Create the Component
```bash
# Create folder
mkdir -p src/app/modules/reports
```

#### 2. Create Component File
Create `src/app/modules/reports/reports.component.ts`:

```typescript
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SectionCardComponent } from '../../shared/components/section-card.component';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, PageHeaderComponent, SectionCardComponent],
  template: `
    <app-page-header
      title="Reports"
      subtitle="Generated reports"
      [breadcrumbs]="[
        { label: 'Home', routerLink: '/' },
        { label: 'Reports', active: true }
      ]"
    ></app-page-header>

    <app-section-card title="Recent Reports">
      <p>Your reports will appear here.</p>
    </app-section-card>
  `,
  styles: [`
    app-section-card {
      display: block;
      margin-bottom: 2rem;
    }
  `],
})
export class ReportsComponent {}
```

#### 3. Add Route
Edit `src/app/app.routes.ts`, add to the children array:

```typescript
{
  path: 'reports',
  component: ReportsComponent,
  data: { title: 'Reports' },
}
```

#### 4. Add Navigation
Edit `src/app/layout/sidebar.component.ts`, add to the nav section:

```typescript
<li class="nav-item">
  <a routerLink="/reports" routerLinkActive="active">
    <span class="nav-icon">📄</span>
    <span class="nav-label">Reports</span>
  </a>
</li>
```

#### 5. Test
Visit: **http://localhost:4200/reports**

---

## Adding API Integration

### Step-by-Step Example: Fetch Kundali Charts

#### 1. Use the Service in Component
```typescript
import { KundaliApiService } from '../../core/services/kundali-api.service';
import { OnInit } from '@angular/core';

export class KundaliComponent implements OnInit {
  kundaliCharts: any[] = [];
  loading = true;
  error = '';

  constructor(private kundaliService: KundaliApiService) {}

  ngOnInit() {
    this.loadCharts();
  }

  loadCharts() {
    this.loading = true;
    this.kundaliService.getKundaliCharts().subscribe({
      next: (response) => {
        this.kundaliCharts = response.data;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message;
        this.loading = false;
      },
    });
  }
}
```

#### 2. Display in Template
```html
<app-loading-spinner *ngIf="loading"></app-loading-spinner>

<div *ngIf="error" class="error-message">{{ error }}</div>

<app-section-card *ngFor="let chart of kundaliCharts" [title]="chart.name">
  <p>DOB: {{ chart.dateOfBirth | date }}</p>
  <p>Location: {{ chart.placeOfBirth }}</p>
</app-section-card>
```

---

## Styling

### Using Theme Variables

All theme variables are defined in `src/app/config/theme/theme.scss`.

#### In Components (SCSS)
```scss
@import '../../../config/theme/theme.scss';

.my-element {
  background: $color-primary;
  color: $color-white;
  padding: $spacing-lg;
  border-radius: $border-radius-lg;
  @include card-hover;

  @include respond-to('md') {
    padding: $spacing-md;
  }
}
```

#### Available Variables
- **Colors**: `$color-primary`, `$color-secondary`, `$color-accent`, `$color-success`, etc.
- **Typography**: `$font-size-base`, `$font-weight-medium`, `$font-family-base`
- **Spacing**: `$spacing-sm`, `$spacing-md`, `$spacing-lg`, `$spacing-xl`
- **Radius**: `$border-radius-md`, `$border-radius-lg`, `$border-radius-full`
- **Shadows**: `$shadow-sm`, `$shadow-md`, `$shadow-lg`, `$shadow-xl`

#### Available Mixins
```scss
@include flex-center;        // Flexbox center
@include flex-between;       // Space between layout
@include card-base;          // Card styling
@include card-hover;         // Card with hover effect
@include transition($props); // Smooth transitions
@include text-truncate;      // Single line truncate
@include text-clamp(2);      // Multi-line truncate
@include respond-to('md');   // Media query mixin
```

---

## Common Tasks

### Task 1: Display Loading State
```typescript
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner.component';

// In component template:
<app-loading-spinner 
  *ngIf="loading" 
  [message]="'Fetching your charts...'"
></app-loading-spinner>
```

### Task 2: Create a Modal/Dialog
Currently, use Angular CDK or PrimeNG (to be added). For now, use:
```typescript
// Simple confirm
if (confirm('Are you sure?')) {
  this.deleteChart(chartId);
}
```

### Task 3: Format Date Display
```typescript
import { formatDateTime } from '../../shared/utils/common.utils';

dateString: string = formatDateTime(new Date(), 'long');
// Output: "May 20, 2026, 03:30 PM"
```

### Task 4: Handle Form Validation
```typescript
import { isValidEmail } from '../../shared/utils/common.utils';

if (isValidEmail(this.form.email)) {
  this.submitForm();
}
```

### Task 5: Calculate Age
```typescript
import { calculateAge } from '../../shared/utils/common.utils';

userAge: number = calculateAge('1990-05-15');
// Output: 36
```

---

## Debugging

### Enable Console Logs
In any service:
```typescript
console.log('API Response:', response);
```

### Check Network Requests
1. Open Developer Tools (F12)
2. Go to Network tab
3. Make API calls
4. Inspect requests/responses

### Common Issues

| Issue | Solution |
|-------|----------|
| CORS error | Ensure backend allows frontend origin |
| 404 on routes | Check routing config in `app.routes.ts` |
| Components not showing | Import in component's `imports` array |
| Styles not applying | Check SCSS import paths and variable names |
| API calls getting 401 | Check auth token/credentials |

---

## Build & Deployment

### Build for Production
```bash
npm run build
```

Outputs to: `dist/kundali-portal-ui/`

### Optimize Production Build
- Minification ✓ (automatic)
- Tree-shaking ✓ (unused code removed)
- Bundle analysis: `npm install -g webpack-bundle-analyzer`

### Deploy to Web Server

#### Option 1: Firebase Hosting
```bash
npm install -g firebase-tools
firebase init hosting
npm run build
firebase deploy
```

#### Option 2: Traditional Web Server (Nginx)
```nginx
server {
  listen 80;
  server_name yourdomain.com;
  root /var/www/kundali-portal-ui/dist/kundali-portal-ui;

  location / {
    try_files $uri $uri/ /index.html;
  }
}
```

#### Option 3: Docker
```dockerfile
FROM node:18-alpine AS builder
WORKDIR /app
COPY . .
RUN npm install && npm run build

FROM nginx:alpine
COPY --from=builder /app/dist/kundali-portal-ui /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

---

## Testing

### Run Unit Tests
```bash
npm test
```

### Example Test
```typescript
describe('DashboardComponent', () => {
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load kundali charts', () => {
    expect(component.kundaliCharts.length).toBeGreaterThan(0);
  });
});
```

---

## Performance Tips

1. **Use OnPush Change Detection**:
```typescript
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
})
```

2. **Lazy Load Modules**:
```typescript
{
  path: 'reports',
  loadComponent: () => import('./reports/reports.component').then(m => m.ReportsComponent)
}
```

3. **Optimize Image Loading**:
```html
<img [ngSrc]="imageUrl" width="300" height="200" priority />
```

4. **Use TrackBy in Lists**:
```typescript
trackByCharId(index: number, chart: Kundali) {
  return chart.id;
}
```

---

## Resources

- **Angular Docs**: https://angular.io/docs
- **NgRx State Management**: https://ngrx.io/
- **RxJS Operators**: https://rxjs.dev/
- **TypeScript**: https://www.typescriptlang.org/
- **SCSS Reference**: https://sass-lang.com/

---

## Next Steps

1. ✅ **Architecture Review** - Read [ARCHITECTURE.md](./ARCHITECTURE.md)
2. ✅ **Environment Setup** - Configure backend URL
3. ✅ **Create Your First Page** - Follow "Creating Your First Component"
4. ✅ **Integrate API** - Connect to backend services
5. ⬜ **Add Authorization** - Implement login/auth guard
6. ⬜ **Add Charts** - Integrate chart library (Chart.js, D3.js)
7. ⬜ **Deploy** - Build and deploy to production

---

## Support

For issues or questions:
1. Check [ARCHITECTURE.md](./ARCHITECTURE.md) for design patterns
2. Review example components in `src/app/modules/`
3. Check service patterns in `src/app/core/services/`
4. Review shared utilities in `src/app/shared/utils/`

---

**Happy Coding! 🚀**
