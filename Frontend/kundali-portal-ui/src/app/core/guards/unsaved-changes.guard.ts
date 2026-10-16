import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { ConfirmDialogService } from '../../shared/components/confirm-dialog/confirm-dialog.service';

export interface UnsavedChangesAware {
  hasUnsavedChanges(): boolean;
  readonly unsavedChangesMessage?: string;
}

export const unsavedChangesGuard: CanDeactivateFn<UnsavedChangesAware> = (component) => {
  if (!component.hasUnsavedChanges()) {
    return true;
  }

  const confirmDialog = inject(ConfirmDialogService);
  const transloco = inject(TranslocoService);
  return confirmDialog.confirm({
    title: transloco.translate('confirm.leaveTitle'),
    message: component.unsavedChangesMessage || transloco.translate('confirm.leaveMessage'),
    confirmLabel: transloco.translate('confirm.leave'),
    cancelLabel: transloco.translate('confirm.cancel'),
  });
};

