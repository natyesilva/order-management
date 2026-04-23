import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { createOrder } from '../lib/api.js'
import { useI18n } from '../lib/i18n/I18nProvider.jsx'

export default function OrderCreatePage() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const [customer, setCustomer] = useState('')
  const [product, setProduct] = useState('')
  const [value, setValue] = useState('10.00')
  const [quantity, setQuantity] = useState('1')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)

  async function onSubmit(e) {
    e.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      const payload = {
        customer,
        product,
        value: Number(value),
        quantity: Number(quantity),
      }
      const created = await createOrder(payload)
      navigate(`/orders/${created.id}`)
    } catch (err) {
      setError(err.message || String(err))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="mx-auto max-w-xl space-y-4">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-ink-900">{t('newOrderTitle')}</h1>
        <p className="mt-1 text-sm text-ink-700">{t('newOrderSubtitle')}</p>
      </div>

      <form
        onSubmit={onSubmit}
        className="rounded-2xl border border-black/5 bg-white/70 p-5 shadow-sm ring-1 ring-black/5"
      >
        <label className="block">
          <span className="text-sm font-medium text-ink-900">{t('customer')}</span>
          <input
            value={customer}
            onChange={(e) => setCustomer(e.target.value)}
            placeholder="Acme Ltda."
            className="mt-1 w-full rounded-xl border border-black/10 bg-white px-3 py-2 text-sm outline-none ring-teal-200 focus:ring-4"
            required
            minLength={2}
            maxLength={200}
          />
        </label>

        <label className="mt-4 block">
          <span className="text-sm font-medium text-ink-900">{t('product')}</span>
          <input
            value={product}
            onChange={(e) => setProduct(e.target.value)}
            placeholder="Produto X"
            className="mt-1 w-full rounded-xl border border-black/10 bg-white px-3 py-2 text-sm outline-none ring-teal-200 focus:ring-4"
            required
            minLength={2}
            maxLength={200}
          />
        </label>

        <label className="mt-4 block">
          <span className="text-sm font-medium text-ink-900">{t('value')}</span>
          <input
            value={value}
            onChange={(e) => setValue(e.target.value)}
            type="number"
            step="0.01"
            min="0.01"
            className="mt-1 w-full rounded-xl border border-black/10 bg-white px-3 py-2 text-sm outline-none ring-teal-200 focus:ring-4"
            required
          />
        </label>

        <label className="mt-4 block">
          <span className="text-sm font-medium text-ink-900">{t('quantity')}</span>
          <input
            value={quantity}
            onChange={(e) => setQuantity(e.target.value)}
            type="number"
            step="1"
            min="1"
            className="mt-1 w-full rounded-xl border border-black/10 bg-white px-3 py-2 text-sm outline-none ring-teal-200 focus:ring-4"
            required
          />
        </label>

        {error ? (
          <div className="mt-4 rounded-xl bg-red-50 px-3 py-2 text-sm text-red-800 ring-1 ring-red-200">
            {error}
          </div>
        ) : null}

        <button
          type="submit"
          disabled={submitting}
          className="mt-5 inline-flex w-full items-center justify-center rounded-xl bg-ink-900 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-ink-700 disabled:opacity-60"
        >
          {submitting ? t('creating') : t('create')}
        </button>
      </form>
    </div>
  )
}
