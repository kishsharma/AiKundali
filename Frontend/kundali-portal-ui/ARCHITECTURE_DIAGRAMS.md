# Kundali Portal Architecture Diagrams

## 1. High-Level Application Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   KUNDALI PORTAL UI                      │
└─────────────────────────────────────────────────────────┘
         │
         ├─── PRESENTATION LAYER
         │    ├─ LayoutComponent (Shell)
         │    │  ├─ NavbarComponent (Top nav)
         │    │  ├─ SidebarComponent (Side nav)
         │    │  └─ router-outlet (Content area)
         │    │
         │    └─ Feature Components
         │       ├─ DashboardComponent
         │       ├─ KundaliComponent
         │       ├─ CompatibilityComponent
         │       ├─ PanchangComponent
         │       └─ DashaComponent
         │
         ├─── SHARED LAYER
         │    ├─ Reusable Components
         │    │  ├─ LoadingSpinnerComponent
         │    │  ├─ SectionCardComponent
         │    │  └─ PageHeaderComponent
         │    │
         │    ├─ Models
         │    │  └─ api.models.ts (DTOs)
         │    │
         │    └─ Utilities
         │       └─ common.utils.ts (Helper functions)
         │
         ├─── CORE LAYER
         │    ├─ Services
         │    │  ├─ BaseApiService (Base HTTP)
         │    │  ├─ KundaliApiService
         │    │  ├─ CompatibilityApiService
         │    │  ├─ PanchangApiService
         │    │  └─ DashaApiService
         │    │
         │    ├─ Interceptors (Ready for auth)
         │    └─ Guards (Ready for auth)
         │
         ├─── CONFIG LAYER
         │    ├─ Theme Configuration
         │    │  ├─ theme.config.ts (TS object)
         │    │  └─ theme.scss (SCSS variables)
         │    │
         │    └─ Environment Configuration
         │       ├─ environment.ts (Development)
         │       ├─ environment.prod.ts (Production)
         │       └─ environment.staging.ts (Staging)
         │
         └─── BACKEND API (.NET)
              └─ http://localhost:5000/api
```

---

## 2. Component Hierarchy

```
App
└── LayoutComponent (Main Shell)
    ├── NavbarComponent (Sticky Top)
    │   ├── Logo/Brand
    │   ├── Nav Links (Dashboard, Kundali, Compatibility, etc.)
    │   ├── Action Buttons (Notifications, Theme, User Menu)
    │   └── Mobile Menu Hamburger
    │
    ├── SidebarComponent (Left Navigation) [Responsive]
    │   ├── Main Section
    │   │   ├── Dashboard Link
    │   │   ├── Kundali Link
    │   │   └── Compatibility Link
    │   ├── Astrology Section
    │   │   ├── Panchang Link
    │   │   └── Dasha Link
    │   ├── Settings Section
    │   │   ├── Preferences Link
    │   │   └── Help & Support Link
    │   └── Footer
    │       └── Logout Button
    │
    └── Content Area (router-outlet)
        ├── DashboardComponent
        │   ├── PageHeaderComponent
        │   ├── StatsCards
        │   └── QuickActionsGrid
        │
        ├── KundaliComponent
        │   ├── PageHeaderComponent
        │   ├── KundaliFormCard
        │   └── YourChartsCard
        │
        ├── CompatibilityComponent
        │   ├── PageHeaderComponent
        │   ├── AnalyzerCard
        │   └── HistoryCard
        │
        ├── PanchangComponent
        │   ├── PageHeaderComponent
        │   ├── DateSelectorCard
        │   └── PanchangDetailsCard
        │
        └── DashaComponent
            ├── PageHeaderComponent
            ├── ChartSelectorCard
            ├── TimelineCard
            └── PredictionsCard
```

---

## 3. Data Flow (Component → Service → API)

```
USER INTERACTION
       ↓
   Component
   ├─ Event triggered (button click, form submit, etc.)
   ├─ Calls service method
   └─ Subscribes to Observable
       ↓
   Service
   ├─ Receives component call
   ├─ Makes HTTP request via BaseApiService
   ├─ Adds business logic
   └─ Returns Observable<ApiResponse<Data>>
       ↓
   BaseApiService
   ├─ Constructs HTTP request
   ├─ Adds timeout & error handling
   ├─ Sends to backend API
   └─ Catches & transforms errors
       ↓
   BACKEND API (.NET)
   ├─ Receives HTTP request
   ├─ Processes request
   └─ Returns ApiResponse<Data>
       ↓
   Error Handler (or Success)
   ├─ If success → passes data to component
   ├─ If error → formats error message
   └─ Component displays result
       ↓
   USER SEES RESULT
```

---

## 4. Routing Flow

```
User enters URL
       ↓
Angular Router checks app.routes.ts
       ↓
┌─ Route: / → redirect to /dashboard
├─ Route: /dashboard → DashboardComponent (in LayoutComponent)
├─ Route: /kundali → KundaliComponent (in LayoutComponent)
├─ Route: /compatibility → CompatibilityComponent (in LayoutComponent)
├─ Route: /panchang → PanchangComponent (in LayoutComponent)
├─ Route: /dasha → DashaComponent (in LayoutComponent)
└─ Route: ** (wildcard) → redirect to /dashboard
       ↓
LayoutComponent renders with selected route component
       ↓
NavbarComponent & SidebarComponent stay visible
Content area updates with routed component
```

---

## 5. Service Inheritance Hierarchy

```
BaseApiService (Abstract base)
├─ Provides: get(), post(), put(), patch(), delete()
├─ Provides: error handling
├─ Provides: timeout management
└─ Uses: HttpClient
    │
    ├── KundaliApiService
    │   ├─ getKundaliCharts()
    │   ├─ generateKundali()
    │   ├─ updateKundali()
    │   └─ deleteKundali()
    │
    ├── CompatibilityApiService
    │   ├─ analyzeCompatibility()
    │   ├─ getCompatibilityHistory()
    │   └─ deleteCompatibilityAnalysis()
    │
    ├── PanchangApiService
    │   ├─ getPanchang()
    │   ├─ getPanchangRange()
    │   └─ getAuspiciousTimings()
    │
    └── DashaApiService
        ├─ getDashaAnalysis()
        ├─ getCurrentDasha()
        └─ getDashaPredictions()
```

---

## 6. Folder Structure Tree

```
kundali-portal-ui/
├── src/
│   ├── app/
│   │   ├── core/
│   │   │   ├── services/
│   │   │   │   ├── base-api.service.ts ⭐
│   │   │   │   ├── kundali-api.service.ts
│   │   │   │   ├── compatibility-api.service.ts
│   │   │   │   ├── panchang-api.service.ts
│   │   │   │   └── dasha-api.service.ts
│   │   │   ├── interceptors/  [Ready for auth]
│   │   │   └── guards/       [Ready for auth]
│   │   │
│   │   ├── shared/
│   │   │   ├── components/
│   │   │   │   ├── loading-spinner.component.ts ⭐
│   │   │   │   ├── section-card.component.ts ⭐
│   │   │   │   └── page-header.component.ts ⭐
│   │   │   ├── models/
│   │   │   │   └── api.models.ts
│   │   │   └── utils/
│   │   │       └── common.utils.ts
│   │   │
│   │   ├── layout/
│   │   │   ├── layout.component.ts ⭐ (Main shell)
│   │   │   ├── navbar.component.ts ⭐
│   │   │   └── sidebar.component.ts ⭐
│   │   │
│   │   ├── modules/
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
│   │   ├── config/
│   │   │   ├── theme/
│   │   │   │   ├── theme.config.ts
│   │   │   │   └── theme.scss ⭐
│   │   │   └── environment/
│   │   │       ├── environment.ts
│   │   │       ├── environment.prod.ts
│   │   │       └── environment.staging.ts
│   │   │
│   │   ├── app.routes.ts ⭐
│   │   ├── app.config.ts ⭐
│   │   ├── app.ts (Root component)
│   │   └── app.html
│   │
│   ├── styles.scss (Imports theme.scss)
│   ├── main.ts (Bootstrap)
│   └── index.html
│
├── angular.json
├── package.json
├── tsconfig.json
├── ARCHITECTURE.md ⭐ (Comprehensive guide)
├── GETTING_STARTED.md ⭐ (Setup & tutorials)
└── PROJECT_SUMMARY.md ⭐ (This overview)
```

**⭐ = Key Files to Understand First**

---

## 7. Responsive Breakpoints

```
MOBILE          TABLET          DESKTOP         ULTRAWIDE
xs < 460px      md ≥ 768px      lg ≥ 1024px    2xl ≥ 1536px
    sm
640px - 768px

Menu: Hamburger  →  Full Sidebar  →  Full Sidebar  →  Full Sidebar + Expanded
Cards: 1 column →  2 columns     →  3 columns    →  4 columns
```

---

## 8. Theme Color Palette

```
PRIMARY (Astrology Purple)
  ├─ Light: #9C4EE0
  ├─ Main:  #7E3BA8 ← Most used
  └─ Dark:  #6A2E8F

SECONDARY (Pink)
  ├─ Light: #FF6B9D
  ├─ Main:  #FF1E64
  └─ Dark:  #CC1850

ACCENT (Teal)
  ├─ Light: #00BCD4
  ├─ Main:  #0097A7
  └─ Dark:  #00838F

NEUTRALS
  ├─ White:  #FFFFFF
  ├─ Light:  #F5F5F5
  ├─ Gray:   #E0E0E0 to #424242
  └─ Dark:   #212121

STATUS
  ├─ Success: #4CAF50
  ├─ Warning: #FF9800
  ├─ Error:   #F44336
  └─ Info:    #2196F3
```

---

## 9. API Request/Response Flow

```
COMPONENT
   └─ Calls: this.kundaliService.getKundaliCharts()
       │
       └─→ KUNDALI API SERVICE
           └─ Calls: this.get<Kundali[]>('kundali')
               │
               └─→ BASE API SERVICE
                   ├─ Constructs URL: http://localhost:5000/api/kundali
                   ├─ Makes HttpClient.get() request
                   ├─ Adds timeout(30000ms)
                   ├─ Catches errors
                   └─ Returns Observable<ApiResponse<Kundali[]>>
                       │
                       └─→ HTTP REQUEST
                           │
                           └─→ BACKEND API
                               ├─ GET /api/kundali
                               ├─ Validate user
                               ├─ Query database
                               └─ Return: ApiResponse<Kundali[]>
                                  {
                                    success: true,
                                    data: [...charts],
                                    message: "Success"
                                  }
                               │
                               └─→ RESPONSE
                                   │
                   ┌─────────────────┴─────────────────┐
                   │                                    │
            SUCCESS (200)                         ERROR (400/500)
            │                                        │
            next()                               error()
            │                                        │
            Component receives data             handleError()
            Updates UI                          Logs error
                                               Throws formatted error
```

---

## 10. Development Workflow

```
1. START DEV SERVER
   npm start
   └─ Opens http://localhost:4200
       └─ Shows Dashboard

2. CREATE NEW FEATURE
   ├─ Create component (src/app/modules/[feature]/)
   ├─ Add route (app.routes.ts)
   ├─ Add navigation link (sidebar/navbar)
   └─ Import shared components as needed

3. INTEGRATE API
   ├─ Create/update service (core/services/)
   ├─ Inject in component
   ├─ Subscribe to Observable
   ├─ Display data or error
   └─ Handle loading state

4. STYLE
   ├─ Use SCSS variables ($color-primary, $spacing-lg, etc.)
   ├─ Use mixins (@include card-base, @include respond-to('md'))
   ├─ Use theme config (themeConfig.colors.primary)
   └─ Changes apply instantly with hot reload

5. TEST & BUILD
   ├─ npm test (if configured)
   ├─ npm run build (creates dist/)
   ├─ Deploy to hosting

6. DEPLOY
   └─ Copy dist/kundali-portal-ui/ to web server
       └─ Serve with SPA routing (all routes → index.html)
```

---

## 11. Key Files Overview

| File | Type | Purpose |
|------|------|---------|
| `app.routes.ts` | Config | Define all routes |
| `app.config.ts` | Config | Provide HttpClient, Router |
| `layout.component.ts` | Component | Main shell wrapper |
| `base-api.service.ts` | Service | HTTP base layer |
| `theme.scss` | Style | Centralized variables |
| `environment.ts` | Config | Dev backend URL |
| `common.utils.ts` | Util | Helper functions |

---

This visual guide helps understand the relationships between layers, components, services, and data flow. Refer back to [ARCHITECTURE.md](./ARCHITECTURE.md) for detailed implementation patterns.

