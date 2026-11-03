import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, of, switchMap, takeUntil } from 'rxjs';
import { LocationService, LocationSearchResult } from '../../core/services/location.service';
import { AppointmentRequest, CrmDashboardSummary, CrmStatus, PractitionerAppointment, PractitionerClient, PractitionerClientChart, PractitionerCrmService } from '../../core/services/practitioner-crm.service';
import { KundaliService } from '../kundali/kundali.service';
import { NotificationService } from '../../shared/services/notification.service';

@Component({
  selector: 'app-clients',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  template: `
    <section class="crm-page">
      <header class="crm-heading">
        <div><p class="eyebrow">Astrologer workspace</p><h1>CRM</h1><p class="subhead">Manage your clients, charts and consultations in one place.</p></div>
        <a routerLink="/app/clients" class="back-link" *ngIf="clientId && client">← CRM dashboard</a>
      </header>

      <p class="state-card" *ngIf="loading">Loading clients…</p>
      <p class="state-card error" *ngIf="errorMessage">{{ errorMessage }}</p>

      <ng-container *ngIf="!loading && !clientId">
        <section class="crm-metrics" aria-label="CRM summary">
          <article class="metric-card"><span>Active clients</span><strong>{{ summary?.activeClients ?? 0 }}</strong></article>
          <article class="metric-card"><span>All saved charts</span><strong>{{ summary?.allSavedCharts ?? 0 }}</strong></article>
          <article class="metric-card"><span>CRM-linked charts</span><strong>{{ summary?.linkedClientCharts ?? 0 }}</strong></article>
          <article class="metric-card"><span>Upcoming consultations</span><strong>{{ summary?.upcomingConsultations ?? 0 }}</strong></article>
          <article class="metric-card"><span>Completed consultations (last 30 days)</span><strong>{{ summary?.completedConsultationsLast30Days ?? 0 }}</strong></article>
          <article class="metric-card"><span>Today’s consultations</span><strong>{{ summary?.todaysConsultations ?? 0 }}</strong></article>
        </section>
        <section class="crm-toolbar">
          <input type="search" [value]="searchText" (input)="searchText = $any($event.target).value" placeholder="Search clients by name, phone, or email" aria-label="Search clients" />
          <button class="primary" type="button" (click)="beginCreate()">Add client</button>
        </section>

        <section class="crm-card client-table-card">
          <div class="section-head"><div><p class="eyebrow">Client directory</p><h2>Clients</h2></div><span class="count-badge">{{ filteredClients.length }}</span></div>
          <div class="table-scroll" *ngIf="filteredClients.length">
            <table class="crm-client-table"><thead><tr><th>Client</th><th>Contact</th><th>Birth details</th><th>Topic</th><th>Status</th><th>Charts</th><th>Next consultation</th><th>Actions</th></tr></thead>
              <tbody><tr *ngFor="let item of filteredClients">
                <td><a class="client-name-link" [routerLink]="['/app/clients', item.id]"><strong>{{ displayName(item) }}</strong></a></td>
                <td>{{ item.phoneNumber || '—' }}<small>{{ item.email || '' }}</small></td>
                <td>{{ item.dateOfBirth | date:'dd-MMM-yyyy':istTimeZone }} · {{ formatBirthTime(item.timeOfBirth) }}<small>{{ item.birthPlace }}</small></td>
                <td>{{ item.topic || '—' }}</td>
                <td><span class="status-pill" [attr.data-status]="item.status">{{ item.status }}</span></td>
                <td>{{ item.linkedChartCount }}</td>
                <td *ngIf="item.nextAppointment as next">{{ formatAppointmentLocal(next.startsAtLocal) }} {{ next.timeZoneId }}<small>{{ next.topic || next.status }}</small></td><td *ngIf="!item.nextAppointment">—</td>
                <td><button type="button" class="secondary compact-action" (click)="beginAppointmentFor(item)">Add consultation</button></td>
              </tr></tbody>
            </table>
          </div>
          <p class="state-card" *ngIf="!filteredClients.length">{{ searchText ? 'No clients match your search.' : 'No clients yet. Add your first client to get started.' }}</p>
        </section>

        <div class="modal-backdrop" *ngIf="showClientForm" (click)="cancelClientForm()" (keydown.escape)="cancelClientForm()">
          <section class="crm-modal" role="dialog" aria-modal="true" aria-labelledby="client-modal-title" (click)="$event.stopPropagation()">
            <form [formGroup]="clientForm" (ngSubmit)="saveClient()">
              <div class="modal-heading"><div><p class="eyebrow">CRM</p><h2 id="client-modal-title">{{ editingClient ? 'Edit client' : 'Add client' }}</h2></div><button class="icon-close" type="button" aria-label="Close" (click)="cancelClientForm()">×</button></div>
              <p class="form-hint">Birth details are used to prefill a Kundali. Enter a phone number, email, or both.</p>
              <div class="form-grid compact-grid">
                <label>Name<input formControlName="fullName" maxlength="150" required /></label>
                <label>Gender<select formControlName="gender" required><option value="">Select</option><option value="Male">Male</option><option value="Female">Female</option><option value="Other">Other</option></select></label>
                <label>Birth date<input type="date" formControlName="dateOfBirth" required /></label>
                <label>Birth time<input type="time" formControlName="timeOfBirth" required /></label>
                <label class="wide location-field">Birth place<input formControlName="birthPlace" autocomplete="off" required placeholder="Search for a city" /><div class="place-results" *ngIf="placeResults.length"><button type="button" *ngFor="let place of placeResults" (click)="selectPlace(place)">{{ place.displayName }}</button></div></label>
                <label>Email<input type="email" formControlName="email" maxlength="150" autocomplete="email" /></label>
                <label>Mobile number<input type="tel" formControlName="phoneNumber" maxlength="30" autocomplete="tel" /></label>
                <label>Topic<input formControlName="topic" maxlength="200" placeholder="What would the client like guidance on?" /></label>
                <label>Status<select formControlName="status"><option *ngFor="let status of crmStatuses" [value]="status">{{ status }}</option></select></label>
                <label class="wide">Notes<textarea formControlName="notes" maxlength="2000" rows="2"></textarea></label>
                <details class="wide additional-fields"><summary>Additional birth details</summary><div class="form-grid compact-grid">
                  <label>Surname<input formControlName="surname" maxlength="150" /></label><label>Father’s name<input formControlName="fatherName" maxlength="150" /></label>
                  <label>Mother’s name<input formControlName="motherName" maxlength="150" /></label><label>Gotra<input formControlName="gotra" maxlength="100" /></label>
                  <label>Latitude<input type="number" step="0.000001" formControlName="latitude" /></label><label>Longitude<input type="number" step="0.000001" formControlName="longitude" /></label>
                </div></details>
              </div>
              <div class="form-actions"><button type="button" class="quiet" (click)="cancelClientForm()">Cancel</button><button class="primary" type="submit" [disabled]="saving">{{ saving ? 'Saving…' : 'Save client' }}</button></div>
            </form>
          </section>
        </div>
        <div class="modal-backdrop" *ngIf="showAppointmentForm && client" (click)="showAppointmentForm = false">
          <section class="crm-modal consultation-modal" role="dialog" aria-modal="true" aria-labelledby="consultation-modal-title" (click)="$event.stopPropagation()">
            <form [formGroup]="appointmentForm" (ngSubmit)="saveAppointment()">
              <div class="modal-heading"><div><p class="eyebrow">{{ displayName(client) }}</p><h2 id="consultation-modal-title">{{ editingAppointment ? 'Edit consultation' : 'Add consultation' }}</h2></div><button class="icon-close" type="button" aria-label="Close" (click)="showAppointmentForm = false">×</button></div>
              <div class="form-grid compact-grid">
                <label>Topic<input formControlName="topic" maxlength="200" /></label>
                <label>Status<select formControlName="status"><option *ngFor="let status of crmStatuses" [value]="status">{{ status }}</option></select></label>
                <label>Start<input type="datetime-local" formControlName="startsAtLocal" required /></label>
                <label>End<input type="datetime-local" formControlName="endsAtLocal" required /></label>
                <label>Time zone<input formControlName="timeZoneId" readonly /></label>
                <label>Reminder<select formControlName="reminderLeadMinutes"><option [ngValue]="60">1 hour before</option><option [ngValue]="15">15 minutes before</option><option [ngValue]="30">30 minutes before</option><option [ngValue]="120">2 hours before</option><option [ngValue]="1440">24 hours before</option><option [ngValue]="null">No reminder</option></select></label>
                <label class="wide">Chart<select formControlName="kundaliId"><option [ngValue]="null">No chart selected</option><option *ngFor="let chart of client.charts" [ngValue]="chart.id">{{ chart.fullName }} · {{ chart.birthPlace }}</option></select></label>
                <label class="wide">Notes<textarea formControlName="notes" maxlength="1000" rows="2"></textarea></label>
              </div>
              <div class="form-actions"><button class="quiet" type="button" (click)="showAppointmentForm = false">Cancel</button><button class="primary" type="submit" [disabled]="saving">{{ saving ? 'Saving…' : 'Save consultation' }}</button></div>
            </form>
          </section>
        </div>
      </ng-container>

      <ng-container *ngIf="!loading && clientId && client">
        <div class="detail-grid" *ngIf="!showClientForm">
          <section class="crm-card profile-card">
            <div class="section-head"><div><p class="eyebrow">Client profile</p><h2>{{ displayName(client) }}</h2></div><button class="quiet" type="button" (click)="beginEdit()" [disabled]="client.isArchived">Edit</button></div>
            <div class="contact-row"><span *ngIf="client.phoneNumber">☎ {{ client.phoneNumber }}</span><span *ngIf="client.email">✉ {{ client.email }}</span><span class="status-pill" [attr.data-status]="client.status">{{ client.status }}</span></div>
            <div class="birth-summary" *ngIf="client.topic || client.notes"><span *ngIf="client.topic"><strong>Topic:</strong> {{ client.topic }}</span><span *ngIf="client.notes"><strong>Notes:</strong> {{ client.notes }}</span></div>
            <div class="birth-summary"><strong>Default birth profile</strong><span>{{ client.gender }} · {{ client.dateOfBirth | date:'dd-MMM-yyyy':istTimeZone }} · {{ formatBirthTime(client.timeOfBirth) }}</span><span>{{ client.birthPlace }}<ng-container *ngIf="client.latitude != null && client.longitude != null"> · {{ client.latitude }}, {{ client.longitude }}</ng-container></span></div>
            <div class="family-summary" *ngIf="client.fatherName || client.motherName || client.gotra"><span *ngIf="client.fatherName">Father: {{ client.fatherName }}</span><span *ngIf="client.motherName">Mother: {{ client.motherName }}</span><span *ngIf="client.gotra">Gotra: {{ client.gotra }}</span></div>
            <div class="form-actions" *ngIf="!client.isArchived"><button class="quiet danger" type="button" (click)="archiveClient()">Archive client</button></div>
            <p class="archived-badge" *ngIf="client.isArchived">Archived client</p>
          </section>

          <section class="crm-card">
            <div class="section-head"><div><p class="eyebrow">Birth charts</p><h2>Linked charts</h2></div><span class="count-badge">{{ client.charts.length }}</span></div>
            <div class="chart-list"><article class="chart-row" *ngFor="let chart of client.charts"><span><strong>{{ chart.fullName }}</strong><small>{{ chart.dateOfBirth | date:'dd-MMM-yyyy':istTimeZone }} · {{ chart.birthPlace }}</small></span><a [routerLink]="['/app/kundali/saved', chart.id]">Open</a><button type="button" class="text-button" (click)="unlinkChart(chart)">Remove link</button></article><p class="muted" *ngIf="!client.charts.length">No charts linked yet.</p></div>
            <div class="link-chart-row" *ngIf="!client.isArchived"><select [value]="selectedChartId" (change)="selectedChartId = $any($event.target).value" aria-label="Select saved chart to link"><option value="">Link an existing saved chart…</option><option *ngFor="let chart of linkableCharts" [value]="chart.id">{{ chart.fullName }} · {{ chart.birthPlace }}</option></select><button type="button" class="secondary" (click)="linkSelectedChart()" [disabled]="!selectedChartId">Link chart</button><a routerLink="/app/kundali">Create a chart</a></div>
          </section>

          <section class="crm-card appointments-card">
            <div class="section-head"><div><p class="eyebrow">Schedule</p><h2>Consultations</h2></div><button class="primary" type="button" *ngIf="!client.isArchived" (click)="beginAppointment()">Add consultation</button></div>
            <form class="appointment-form" *ngIf="showAppointmentForm" [formGroup]="appointmentForm" (ngSubmit)="saveAppointment()">
              <div class="form-grid">
                <label>Start<input type="datetime-local" formControlName="startsAtLocal" required /></label><label>End<input type="datetime-local" formControlName="endsAtLocal" required /></label>
                <label>Time zone<input formControlName="timeZoneId" readonly /></label>
                <label>Reminder<select formControlName="reminderLeadMinutes"><option [ngValue]="60">1 hour before</option><option [ngValue]="15">15 minutes before</option><option [ngValue]="30">30 minutes before</option><option [ngValue]="120">2 hours before</option><option [ngValue]="1440">24 hours before</option><option [ngValue]="null">No reminder</option></select></label>
                <label>Topic<input formControlName="topic" maxlength="200" /></label><label>Status<select formControlName="status"><option *ngFor="let status of crmStatuses" [value]="status">{{ status }}</option></select></label>
                <label>Chart for this consultation<select formControlName="kundaliId"><option [ngValue]="null">No chart selected</option><option *ngFor="let chart of client.charts" [ngValue]="chart.id">{{ chart.fullName }} · {{ chart.birthPlace }}</option></select></label>
                <label class="wide">Notes <span>Optional</span><textarea formControlName="notes" maxlength="1000" rows="2"></textarea></label>
              </div>
              <div class="form-actions"><button class="quiet" type="button" (click)="showAppointmentForm = false">Cancel</button><button class="primary" type="submit" [disabled]="saving">Save consultation</button></div>
            </form>
            <div class="appointment-list"><article class="appointment-row" *ngFor="let appointment of client.appointments" [id]="'appointment-' + appointment.id" [class.cancelled]="appointment.status === 'Cancelled'" [class.notification-target]="appointment.id === requestedAppointmentId" tabindex="-1">
              <span class="appointment-date"><strong>{{ formatAppointmentLocal(appointment.startsAtLocal) }}</strong><small>Until {{ formatAppointmentLocal(appointment.endsAtLocal) }} · {{ appointment.timeZoneId }}</small></span>
              <span class="status-pill" [attr.data-status]="appointment.status">{{ appointment.status }}</span><span class="appointment-chart" *ngIf="appointment.topic">Topic: {{ appointment.topic }}</span><span class="appointment-chart" *ngIf="appointment.kundaliName">Chart: {{ appointment.kundaliName }}</span>
              <span class="overlap-warning" *ngIf="appointment.overlapsExisting">Overlaps another consultation</span>
              <span class="appointment-actions" *ngIf="appointment.status === 'New' || appointment.status === 'Consultation Pending'"><button class="text-button" type="button" (click)="editAppointment(appointment)">Edit</button><button class="text-button" type="button" (click)="completeAppointment(appointment)">Mark complete</button><button class="text-button danger" type="button" (click)="cancelAppointment(appointment)">Cancel time</button></span>
            </article><p class="muted" *ngIf="!client.appointments.length">No consultation times recorded.</p></div>
          </section>
        </div>
        <form class="crm-card client-form" *ngIf="showClientForm" [formGroup]="clientForm" (ngSubmit)="saveClient()">
          <h2>Edit client</h2><p class="form-hint">At least one phone number or email is required.</p>
          <div class="form-grid">
            <label>Full name<input formControlName="fullName" maxlength="150" required /></label><label>Surname<input formControlName="surname" maxlength="150" /></label>
            <label>Gender<select formControlName="gender" required><option value="">Select</option><option value="Male">Male</option><option value="Female">Female</option></select></label><label>Birth date<input type="date" formControlName="dateOfBirth" required /></label>
            <label>Birth time<input type="time" formControlName="timeOfBirth" required /></label><label class="wide location-field">Birth place<input formControlName="birthPlace" autocomplete="off" required /><div class="place-results" *ngIf="placeResults.length"><button type="button" *ngFor="let place of placeResults" (click)="selectPlace(place)">{{ place.displayName }}</button></div></label>
            <label>Latitude<input type="number" step="0.000001" formControlName="latitude" /></label><label>Longitude<input type="number" step="0.000001" formControlName="longitude" /></label>
            <label>Father’s name<input formControlName="fatherName" maxlength="150" /></label><label>Mother’s name<input formControlName="motherName" maxlength="150" /></label><label>Gotra<input formControlName="gotra" maxlength="100" /></label>
            <label>Phone number<input type="tel" formControlName="phoneNumber" maxlength="30" /></label><label>Email<input type="email" formControlName="email" maxlength="150" /></label>
            <label>Topic<input formControlName="topic" maxlength="200" /></label><label>Status<select formControlName="status"><option *ngFor="let status of crmStatuses" [value]="status">{{ status }}</option></select></label><label class="wide">Notes<textarea formControlName="notes" maxlength="2000" rows="2"></textarea></label>
          </div><div class="form-actions"><button class="quiet" type="button" (click)="cancelClientForm()">Cancel</button><button class="primary" type="submit" [disabled]="saving">Save changes</button></div>
        </form>
      </ng-container>
    </section>
  `,
  styles: [`
    :host{display:block;color:var(--theme-text)}.crm-page{max-width:1360px;margin:0 auto;padding:28px clamp(16px,3vw,38px) 64px}.crm-heading,.crm-toolbar,.section-head,.form-actions,.link-chart-row,.appointment-row,.chart-row{display:flex;align-items:center;justify-content:space-between;gap:14px}.crm-heading{margin-bottom:24px}.crm-heading h1{margin:0;font-size:clamp(28px,4vw,40px)}.subhead,.form-hint,.muted{color:var(--theme-muted,#778096);margin:6px 0 0}.eyebrow{text-transform:uppercase;letter-spacing:.12em;font-size:11px;font-weight:800;color:var(--theme-accent);margin:0 0 6px}.crm-toolbar{margin:18px 0 16px}.crm-toolbar input{flex:1}.client-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(min(100%,330px),1fr));gap:12px}.client-card{display:flex;align-items:center;gap:13px;text-align:left;padding:16px;border:1px solid var(--theme-line);border-radius:16px;background:var(--theme-surface);color:inherit;cursor:pointer}.client-card:hover,.client-card:focus-visible{border-color:var(--theme-accent);box-shadow:0 8px 28px color-mix(in srgb,var(--theme-accent) 12%,transparent)}.client-avatar{display:grid;place-items:center;width:42px;height:42px;border-radius:50%;background:color-mix(in srgb,var(--theme-accent) 15%,var(--theme-surface));color:var(--theme-accent);font-weight:800}.client-card-copy{display:grid;gap:4px;flex:1;min-width:0}.client-card-copy small,.chart-row small,.appointment-date small{color:var(--theme-muted,#778096)}.arrow{font-size:20px;color:var(--theme-accent)}.crm-card,.state-card{padding:20px;border:1px solid var(--theme-line);border-radius:18px;background:var(--theme-surface);box-shadow:0 12px 30px rgba(20,27,45,.04)}.state-card{margin:0 0 14px}.state-card.error{border-color:var(--theme-danger);color:var(--theme-danger)}.crm-metrics{display:grid;grid-template-columns:repeat(6,minmax(0,1fr));gap:12px}.metric-card{display:grid;gap:10px;min-width:0;padding:16px;border:1px solid var(--theme-line);border-radius:16px;background:var(--theme-surface);box-shadow:0 8px 24px rgba(20,27,45,.04)}.metric-card span{color:var(--theme-muted,#778096);font-size:12px;line-height:1.35}.metric-card strong{color:var(--theme-accent);font-size:clamp(23px,2.5vw,32px);line-height:1}.client-table-card{padding:16px}.crm-card h2{margin:0;font-size:21px}.table-scroll{width:100%;overflow:auto;border:1px solid var(--theme-line);border-radius:12px}.crm-client-table{width:100%;border-collapse:collapse;min-width:1050px;font-size:13px}.crm-client-table th,.crm-client-table td{padding:11px 12px;border-bottom:1px solid var(--theme-line);text-align:left;vertical-align:middle}.crm-client-table th{background:color-mix(in srgb,var(--theme-accent) 7%,var(--theme-surface));color:var(--theme-muted,#778096);font-size:11px;text-transform:uppercase;letter-spacing:.06em;white-space:nowrap}.crm-client-table tr:last-child td{border-bottom:0}.crm-client-table td small{display:block;margin-top:3px;color:var(--theme-muted,#778096);font-size:11px}.client-name-link{color:var(--theme-text);text-decoration:none}.client-name-link:hover{color:var(--theme-accent);text-decoration:underline}.compact-action{padding:7px 10px;white-space:nowrap}.detail-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));align-items:start;gap:16px}.profile-card,.appointments-card{grid-column:1/-1}.count-badge,.archived-badge{padding:5px 10px;border-radius:999px;background:color-mix(in srgb,var(--theme-accent) 12%,var(--theme-surface));color:var(--theme-accent);font-weight:750}.contact-row,.family-summary,.birth-summary{display:flex;flex-wrap:wrap;gap:10px 18px;margin-top:16px}.birth-summary{display:grid;padding:14px;border-radius:12px;background:color-mix(in srgb,var(--theme-accent) 7%,var(--theme-surface))}.family-summary{color:var(--theme-muted,#778096);font-size:13px}.form-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:13px}.form-grid label{position:relative;display:grid;gap:6px;font-size:13px;font-weight:700}.form-grid label span{font-size:11px;font-weight:500;color:var(--theme-muted,#778096)}.form-grid input,.form-grid select,.form-grid textarea,.crm-toolbar input,.link-chart-row select{width:100%;box-sizing:border-box;padding:10px 11px;border:1px solid var(--theme-line);border-radius:10px;background:var(--theme-input-bg,var(--theme-surface));color:var(--theme-text);font:inherit}.wide{grid-column:1/-1}.form-actions{justify-content:flex-end;margin-top:16px}.primary,.secondary,.quiet{border:1px solid transparent;border-radius:10px;padding:10px 15px;font:inherit;font-weight:750;cursor:pointer}.primary{background:var(--theme-accent);color:var(--theme-bg)}.secondary{background:color-mix(in srgb,var(--theme-accent) 12%,var(--theme-surface));color:var(--theme-accent);border-color:color-mix(in srgb,var(--theme-accent) 35%,var(--theme-line))}.quiet{border-color:var(--theme-line);background:transparent;color:inherit}.quiet.danger,.danger{color:var(--theme-danger)}button:disabled{opacity:.5;cursor:not-allowed}.form-hint{margin:0 0 14px}.place-results{position:absolute;z-index:10;top:100%;left:0;right:0;display:grid;padding:5px;border:1px solid var(--theme-line);border-radius:10px;background:var(--theme-surface);box-shadow:0 8px 22px rgba(0,0,0,.13)}.place-results button{text-align:left;padding:8px;border:0;background:transparent;color:inherit;cursor:pointer}.place-results button:hover{background:color-mix(in srgb,var(--theme-accent) 12%,var(--theme-surface))}.chart-list,.appointment-list{display:grid;gap:10px;margin-top:12px}.chart-row,.appointment-row{justify-content:flex-start;flex-wrap:wrap;padding:12px;border-radius:12px;background:color-mix(in srgb,var(--theme-accent) 5%,var(--theme-surface))}.chart-row>span,.appointment-date{display:grid;gap:4px;flex:1;min-width:130px}.chart-row a,.back-link,.link-chart-row>a{color:var(--theme-accent);font-weight:700;text-decoration:none}.text-button{padding:5px;border:0;background:none;color:var(--theme-accent);font:inherit;font-size:12px;cursor:pointer}.text-button.danger{color:var(--theme-danger)}.link-chart-row{justify-content:flex-start;flex-wrap:wrap;margin-top:14px}.link-chart-row select{flex:1;min-width:230px}.status-pill{padding:4px 9px;border-radius:999px;background:color-mix(in srgb,var(--theme-accent) 12%,var(--theme-surface));font-size:11px;font-weight:800;white-space:nowrap}.status-pill[data-status="Cancelled"]{background:color-mix(in srgb,var(--theme-danger) 12%,var(--theme-surface));color:var(--theme-danger)}.status-pill[data-status="Completed"]{color:var(--theme-muted,#778096)}.appointment-chart,.overlap-warning{font-size:12px}.overlap-warning{color:var(--theme-warning,#b97700);font-weight:700}.appointment-actions{display:flex;gap:8px;margin-left:auto}.appointment-row.cancelled{opacity:.62}.appointment-form{margin:16px 0;padding:14px;border:1px solid var(--theme-line);border-radius:14px}.appointment-form .form-grid{grid-template-columns:repeat(3,minmax(0,1fr))}.back-link{white-space:nowrap}.modal-backdrop{position:fixed;z-index:1000;inset:0;display:grid;place-items:center;padding:16px;background:rgba(15,18,28,.56);backdrop-filter:blur(3px)}.crm-modal{width:min(620px,100%);max-height:min(90vh,820px);overflow:auto;padding:22px;border:1px solid var(--theme-line);border-radius:18px;background:var(--theme-surface);box-shadow:0 24px 80px rgba(0,0,0,.24)}.consultation-modal{width:min(680px,100%)}.modal-heading{display:flex;align-items:start;justify-content:space-between;gap:14px;margin-bottom:10px}.modal-heading h2{margin:0;font-size:22px}.icon-close{width:34px;height:34px;border:1px solid var(--theme-line);border-radius:50%;background:transparent;color:var(--theme-text);font-size:23px;line-height:1;cursor:pointer}.compact-grid{gap:10px 12px}.compact-grid label{gap:4px;font-size:12px}.compact-grid input,.compact-grid select,.compact-grid textarea{padding:8px 10px;border-radius:8px;font-size:13px}.additional-fields{grid-column:1/-1;padding-top:8px;border-top:1px solid var(--theme-line)}.additional-fields summary{margin-bottom:10px;color:var(--theme-accent);font-size:12px;font-weight:700;cursor:pointer}@media(max-width:1100px){.crm-metrics{grid-template-columns:repeat(3,minmax(0,1fr))}}@media(max-width:760px){.detail-grid{grid-template-columns:1fr}.form-grid,.appointment-form .form-grid,.crm-metrics{grid-template-columns:1fr 1fr}.wide{grid-column:1/-1}.crm-page{padding-top:18px}.appointment-actions{width:100%;margin-left:0}.crm-modal{padding:16px}.crm-toolbar{align-items:stretch}.crm-toolbar button{align-self:flex-end}}@media(max-width:460px){.crm-metrics,.compact-grid{grid-template-columns:1fr}.metric-card{padding:12px}.crm-toolbar{align-items:stretch;flex-direction:column}}
  `, '.appointment-row.notification-target{outline:2px solid var(--theme-accent);outline-offset:2px;scroll-margin:24px}']
})
export class ClientsComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly crm = inject(PractitionerCrmService);
  private readonly kundalis = inject(KundaliService);
  private readonly locations = inject(LocationService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroy$ = new Subject<void>();

  clients: PractitionerClient[] = [];
  client: PractitionerClient | null = null;
  clientId: string | null = null;
  searchText = '';
  loading = false;
  saving = false;
  showClientForm = false;
  showAppointmentForm = false;
  editingClient = false;
  errorMessage = '';
  placeResults: LocationSearchResult[] = [];
  savedCharts: any[] = [];
  upcomingAppointments: PractitionerAppointment[] = [];
  summary: CrmDashboardSummary | null = null;
  readonly crmStatuses: Exclude<CrmStatus, 'Cancelled'>[] = ['New', 'Consultation Pending', 'Completed'];
  readonly istTimeZone = '+0530';
  editingAppointment: PractitionerAppointment | null = null;
  selectedChartId = '';
  timeZoneId = Intl.DateTimeFormat().resolvedOptions().timeZone || 'Asia/Kolkata';
  requestedAppointmentId: string | null = null;

  readonly clientForm = this.fb.group({
    fullName: ['', Validators.required], surname: [''], fatherName: [''], motherName: [''], gotra: [''],
    gender: ['', Validators.required], dateOfBirth: ['', Validators.required], timeOfBirth: ['', Validators.required],
    birthPlace: ['', Validators.required], latitude: [null as number | null], longitude: [null as number | null],
    phoneNumber: [''], email: ['', Validators.email], topic: [''], status: ['New' as CrmStatus], notes: ['']
  });

  readonly appointmentForm = this.fb.group({
    startsAtLocal: ['', Validators.required], endsAtLocal: ['', Validators.required], timeZoneId: [this.timeZoneId, Validators.required],
    reminderLeadMinutes: [60 as number | null], kundaliId: [null as string | null], notes: [''], topic: [''], status: ['Consultation Pending' as CrmStatus]
  });

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.clientId = params.get('id');
      this.showClientForm = false;
      this.showAppointmentForm = false;
      this.loadData();
    });
    this.route.queryParamMap.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.requestedAppointmentId = params.get('appointment');
      if (this.client && this.requestedAppointmentId) this.focusRequestedAppointment();
    });
    this.clientForm.controls.birthPlace.valueChanges.pipe(
      debounceTime(350), distinctUntilChanged(), switchMap(value => value && value.length >= 3 ? this.locations.searchLocation(value) : of([])), takeUntil(this.destroy$)
    ).subscribe(results => { this.placeResults = results; this.cdr.markForCheck(); });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  get filteredClients(): PractitionerClient[] {
    const term = this.searchText.trim().toLowerCase();
    if (!term) return this.clients;
    return this.clients.filter(item => `${item.fullName} ${item.surname ?? ''} ${item.phoneNumber ?? ''} ${item.email ?? ''}`.toLowerCase().includes(term));
  }

  get linkableCharts(): any[] { return this.savedCharts.filter(chart => !chart.clientId || chart.clientId === this.client?.id); }

  formatBirthTime(value: string | null | undefined): string {
    if (!value) return '';
    const match = /^(\d{1,2}):(\d{2})/.exec(value);
    if (!match) return value;
    const hour = Number(match[1]);
    const minute = match[2];
    if (hour > 23 || Number(minute) > 59) return value;
    return `${hour % 12 || 12}:${minute} ${hour < 12 ? 'AM' : 'PM'}`;
  }

  formatAppointmentLocal(value: string | null | undefined): string {
    const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/.exec(value ?? '');
    if (!match) return value ?? '';
    const [, year, month, day, hour, minute] = match;
    const wallClock = new Date(Date.UTC(Number(year), Number(month) - 1, Number(day), Number(hour), Number(minute)));
    return new Intl.DateTimeFormat(undefined, {
      timeZone: 'UTC', day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit'
    }).format(wallClock);
  }

  loadData(): void {
    this.loading = true;
    this.errorMessage = '';
    this.crm.getClients().subscribe({
      next: clients => { this.clients = clients; this.cdr.markForCheck(); },
      error: error => this.showError(error, 'Unable to load clients.'),
      complete: () => { this.loading = false; this.cdr.markForCheck(); }
    });
    if (!this.clientId) {
      this.crm.getDashboardSummary(this.timeZoneId).subscribe({
        next: summary => { this.summary = summary; this.cdr.markForCheck(); },
        error: error => this.showError(error, 'Unable to load CRM summary.')
      });
    }
    if (this.clientId) {
      this.crm.getClient(this.clientId).subscribe({
        next: client => { this.client = client; this.loading = false; this.loadSavedCharts(); this.cdr.markForCheck(); },
        error: error => { this.loading = false; this.showError(error, 'Unable to load this client.'); }
      });
    } else {
      this.client = null;
      this.upcomingAppointments = [];
    }
  }

  private loadSavedCharts(): void {
    this.kundalis.getSavedKundalis().subscribe({ next: charts => { this.savedCharts = charts ?? []; this.cdr.markForCheck(); } });
  }

  openClient(client: PractitionerClient): void { this.router.navigate(['/app/clients', client.id]); }
  beginCreate(): void {
    this.editingClient = false;
    this.clientForm.reset({ fullName: '', surname: '', fatherName: '', motherName: '', gotra: '', gender: '', dateOfBirth: '', timeOfBirth: '', birthPlace: '', latitude: null, longitude: null, phoneNumber: '', email: '', topic: '', status: 'New', notes: '' });
    this.showClientForm = true;
  }
  beginEdit(): void {
    if (!this.client) return;
    this.editingClient = true;
    this.clientForm.patchValue({ ...this.client, latitude: this.client.latitude ?? null, longitude: this.client.longitude ?? null });
    this.showClientForm = true;
  }
  cancelClientForm(): void { this.showClientForm = false; this.placeResults = []; }

  selectPlace(place: LocationSearchResult): void {
    this.clientForm.patchValue({ birthPlace: place.displayName, latitude: place.latitude, longitude: place.longitude });
    this.placeResults = [];
  }

  saveClient(): void {
    if (this.clientForm.invalid) { this.clientForm.markAllAsTouched(); return; }
    const value = this.clientForm.getRawValue();
    if (!value.phoneNumber?.trim() && !value.email?.trim()) { this.notifications.warning('Enter a phone number, an email address, or both.'); return; }
    this.saving = true;
    const request = {
      fullName: value.fullName ?? '', surname: value.surname, fatherName: value.fatherName, motherName: value.motherName,
      gotra: value.gotra, gender: value.gender ?? '', dateOfBirth: value.dateOfBirth ?? '',
      timeOfBirth: value.timeOfBirth && /^\d{1,2}:\d{2}$/.test(value.timeOfBirth) ? `${value.timeOfBirth}:00` : value.timeOfBirth ?? '',
      birthPlace: value.birthPlace ?? '', latitude: value.latitude === null ? null : Number(value.latitude),
      longitude: value.longitude === null ? null : Number(value.longitude), phoneNumber: value.phoneNumber, email: value.email,
      topic: value.topic, status: value.status as CrmStatus, notes: value.notes
    };
    const operation = this.editingClient && this.clientId ? this.crm.updateClient(this.clientId, request) : this.crm.createClient(request);
    operation.subscribe({
      next: client => {
        this.saving = false;
        this.notifications.success(this.editingClient ? 'Client details updated.' : 'Client added.');
        this.showClientForm = false;
        if (this.clientId) { this.client = client; this.loadData(); }
        else { this.loadData(); }
      },
      error: error => { this.saving = false; this.showError(error, 'Unable to save this client.'); }
    });
  }

  archiveClient(): void {
    if (!this.client || !window.confirm(`Archive ${this.displayName(this.client)}? Linked charts and appointment history will remain saved.`)) return;
    this.crm.archiveClient(this.client.id).subscribe({ next: () => { this.notifications.success('Client archived.'); this.router.navigate(['/app/clients']); }, error: error => this.showError(error, 'Unable to archive this client.') });
  }

  linkSelectedChart(): void {
    if (!this.client || !this.selectedChartId) return;
    this.crm.linkChart(this.client.id, this.selectedChartId).subscribe({ next: () => { this.selectedChartId = ''; this.notifications.success('Chart linked to client.'); this.loadClient(); this.loadSavedCharts(); }, error: error => this.showError(error, 'Unable to link this chart.') });
  }

  unlinkChart(chart: PractitionerClientChart): void {
    if (!this.client || !window.confirm(`Remove ${chart.fullName} from this client? The saved chart will remain.`)) return;
    this.crm.unlinkChart(this.client.id, chart.id).subscribe({ next: () => { this.notifications.success('Chart link removed.'); this.loadClient(); this.loadSavedCharts(); }, error: error => this.showError(error, 'Unable to remove this chart link.') });
  }

  beginAppointment(): void {
    this.editingAppointment = null;
    this.appointmentForm.reset({ startsAtLocal: '', endsAtLocal: '', timeZoneId: this.timeZoneId, reminderLeadMinutes: 60, kundaliId: null, notes: '', topic: this.client?.topic ?? '', status: 'Consultation Pending' });
    this.showAppointmentForm = true;
  }

  beginAppointmentFor(client: PractitionerClient): void {
    this.editingAppointment = null;
    this.crm.getClient(client.id).subscribe({
      next: detail => {
        this.client = detail;
        this.appointmentForm.reset({ startsAtLocal: '', endsAtLocal: '', timeZoneId: this.timeZoneId, reminderLeadMinutes: 60, kundaliId: null, notes: '', topic: detail.topic ?? '', status: 'Consultation Pending' });
        this.showAppointmentForm = true;
        this.cdr.markForCheck();
      },
      error: error => this.showError(error, 'Unable to load client details for a consultation.')
    });
  }

  editAppointment(appointment: PractitionerAppointment): void {
    if (appointment.status === 'Cancelled' || appointment.status === 'Completed') return;
    this.editingAppointment = appointment;
    this.appointmentForm.reset({
      startsAtLocal: appointment.startsAtLocal, endsAtLocal: appointment.endsAtLocal, timeZoneId: appointment.timeZoneId,
      reminderLeadMinutes: appointment.reminderLeadMinutes ?? null, kundaliId: appointment.kundaliId ?? null, notes: appointment.notes ?? '',
      topic: appointment.topic ?? '', status: appointment.status === 'Scheduled' ? 'Consultation Pending' : appointment.status
    });
    this.showAppointmentForm = true;
  }

  saveAppointment(): void {
    if (!this.client || this.appointmentForm.invalid) { this.appointmentForm.markAllAsTouched(); return; }
    const value = this.appointmentForm.getRawValue();
    if (!value.startsAtLocal || !value.endsAtLocal || value.endsAtLocal <= value.startsAtLocal) { this.notifications.warning('End time must be after start time.'); return; }
    const request: AppointmentRequest = {
      clientId: this.client.id, startsAtLocal: value.startsAtLocal, endsAtLocal: value.endsAtLocal,
      timeZoneId: value.timeZoneId || this.timeZoneId, reminderLeadMinutes: value.reminderLeadMinutes,
      kundaliId: value.kundaliId, notes: value.notes, topic: value.topic, status: value.status as Exclude<CrmStatus, 'Cancelled'>
    };
    this.saving = true;
    const operation = this.editingAppointment
      ? this.crm.updateAppointment(this.editingAppointment.id, request)
      : this.crm.createAppointment(request);
    operation.subscribe({
      next: appointment => {
        this.saving = false; this.showAppointmentForm = false;
        this.editingAppointment = null;
        this.notifications.success(appointment.overlapsExisting ? 'Consultation saved. It overlaps another consultation.' : 'Consultation time saved.');
        if (appointment.overlapsExisting) this.notifications.warning('Check the time window; it overlaps another scheduled consultation.');
        if (this.clientId) this.loadClient();
        else { this.client = null; this.loadData(); }
      },
      error: error => { this.saving = false; this.showError(error, 'Unable to save the consultation.'); }
    });
  }

  cancelAppointment(appointment: PractitionerAppointment): void {
    if (!window.confirm('Cancel this promised consultation time?')) return;
    this.crm.cancelAppointment(appointment.id).subscribe({ next: () => { this.notifications.success('Consultation cancelled.'); this.loadClient(); }, error: error => this.showError(error, 'Unable to cancel the consultation.') });
  }

  completeAppointment(appointment: PractitionerAppointment): void {
    this.crm.completeAppointment(appointment.id).subscribe({ next: () => { this.notifications.success('Consultation marked complete.'); this.loadClient(); }, error: error => this.showError(error, 'Unable to update the consultation.') });
  }

  displayName(client: PractitionerClient): string { return [client.fullName, client.surname].filter(Boolean).join(' '); }
  upcomingCount(client: PractitionerClient): number { const now = Date.now(); return client.appointments.filter(item => (item.status === 'New' || item.status === 'Consultation Pending') && new Date(item.startsAtUtc).getTime() > now).length; }

  private loadClient(): void {
    if (!this.clientId) return;
    this.crm.getClient(this.clientId).subscribe({ next: client => { this.client = client; this.cdr.markForCheck(); this.focusRequestedAppointment(); }, error: error => this.showError(error, 'Unable to refresh this client.') });
  }

  private focusRequestedAppointment(): void {
    if (!this.requestedAppointmentId) return;
    window.setTimeout(() => {
      const element = document.getElementById(`appointment-${this.requestedAppointmentId}`);
      element?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      element?.focus({ preventScroll: true });
    });
  }

  private showError(error: any, fallback: string): void {
    this.loading = false;
    this.errorMessage = error?.error?.message || fallback;
    this.notifications.error(this.errorMessage);
    this.cdr.markForCheck();
  }
}
