import { CanDeactivateFn } from '@angular/router';

export interface UnsavedChangesAware {
  hasUnsavedChanges(): boolean;
  readonly unsavedChangesMessage?: string;
}

export const unsavedChangesGuard: CanDeactivateFn<UnsavedChangesAware> = (component) => {
  if (!component.hasUnsavedChanges()) {
    return true;
  }

  return window.confirm(
    component.unsavedChangesMessage
      || 'You have an unsaved report. Leave this page without saving?'
  );
};

