import './ConfirmDialog.css';

interface ConfirmDialogProps {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: 'danger' | 'safe';
  onConfirm: () => void;
  onCancel: () => void;
}

export function ConfirmDialog({ title, message, confirmLabel = 'Confirm', cancelLabel = 'Cancel', variant = 'danger', onConfirm, onCancel }: ConfirmDialogProps) {
  return (
    <div className="confirm-overlay" onClick={onCancel}>
      <div className="confirm-dialog" onClick={e => e.stopPropagation()}>
        <h3>{title}</h3>
        <p>{message}</p>
        <div className="confirm-actions">
          <button className="confirm-cancel" onClick={onCancel}>{cancelLabel}</button>
          <button className={`confirm-confirm ${variant === 'safe' ? 'safe' : ''}`} onClick={onConfirm}>{confirmLabel}</button>
        </div>
      </div>
    </div>
  );
}
