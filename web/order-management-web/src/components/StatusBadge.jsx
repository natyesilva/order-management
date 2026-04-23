import { useI18n } from '../lib/i18n/I18nProvider.jsx'

const styles = {
  Pending: 'bg-amber-50 text-amber-800 ring-1 ring-amber-200',
  Processing: 'bg-blue-50 text-blue-800 ring-1 ring-blue-200',
  Completed: 'bg-emerald-50 text-emerald-800 ring-1 ring-emerald-200',
}

export default function StatusBadge({ status }) {
  const { t } = useI18n()
  const cls = styles[status] || 'bg-gray-50 text-gray-800 ring-1 ring-gray-200'

  const label =
    status === 'Pending'
      ? t('statusPending')
      : status === 'Processing'
        ? t('statusProcessing')
        : status === 'Completed'
          ? t('statusCompleted')
          : status

  return (
    <span className={`inline-flex items-center rounded-full px-2.5 py-1 text-xs font-medium ${cls}`}>
      {label}
    </span>
  )
}

