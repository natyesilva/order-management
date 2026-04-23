import { useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import StatusBadge from '../components/StatusBadge.jsx'
import { getOrder } from '../lib/api.js'

function money(value) {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' }).format(value)
}

export default function OrderDetailPage() {
  const { id } = useParams()
  const [order, setOrder] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const pollingMs = useMemo(() => {
    const raw = import.meta.env.VITE_POLLING_MS
    const n = raw ? Number(raw) : 1500
    return Number.isFinite(n) && n > 250 ? n : 1500
  }, [])

  async function load() {
    try {
      setError(null)
      const data = await getOrder(id)
      setOrder(data)
    } catch (e) {
      setError(e.message || String(e))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
    const t = setInterval(load, pollingMs)
    return () => clearInterval(t)
  }, [id, pollingMs])

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-2xl font-semibold tracking-tight text-ink-900">Pedido</h1>
            {order?.status ? <StatusBadge status={order.status} /> : null}
          </div>
          <p className="mt-1 font-mono text-xs text-ink-700">{id}</p>
        </div>
        <Link
          to="/orders"
          className="rounded-xl bg-white/70 px-3 py-2 text-sm font-medium text-ink-900 ring-1 ring-black/5 hover:bg-white"
        >
          Voltar
        </Link>
      </div>

      {loading ? (
        <div className="rounded-2xl border border-black/5 bg-white/70 p-5 text-sm text-ink-700 ring-1 ring-black/5">
          Carregando...
        </div>
      ) : error ? (
        <div className="rounded-2xl bg-red-50 p-5 text-sm text-red-800 ring-1 ring-red-200">{error}</div>
      ) : !order ? (
        <div className="rounded-2xl border border-black/5 bg-white/70 p-5 text-sm text-ink-700 ring-1 ring-black/5">
          Não encontrado.
        </div>
      ) : (
        <div className="grid gap-4 lg:grid-cols-3">
          <div className="rounded-2xl border border-black/5 bg-white/70 p-5 shadow-sm ring-1 ring-black/5 lg:col-span-2">
            <div className="grid gap-4 sm:grid-cols-2">
              <div>
                <div className="text-xs font-medium uppercase tracking-wide text-ink-700">Cliente</div>
                <div className="mt-1 text-sm font-medium text-ink-900">{order.customer}</div>
              </div>
              <div>
                <div className="text-xs font-medium uppercase tracking-wide text-ink-700">Produto</div>
                <div className="mt-1 text-sm font-medium text-ink-900">{order.product}</div>
              </div>
              <div>
                <div className="text-xs font-medium uppercase tracking-wide text-ink-700">Valor</div>
                <div className="mt-1 text-sm font-medium text-ink-900">{money(order.value)}</div>
              </div>
              <div>
                <div className="text-xs font-medium uppercase tracking-wide text-ink-700">Criado em</div>
                <div className="mt-1 text-sm font-medium text-ink-900">
                  {new Date(order.createdAt).toLocaleString()}
                </div>
              </div>
            </div>
          </div>

          <div className="rounded-2xl border border-black/5 bg-white/70 p-5 shadow-sm ring-1 ring-black/5">
            <div className="text-sm font-semibold text-ink-900">Histórico de status</div>
            <div className="mt-3 space-y-2">
              {order.statusHistory?.length ? (
                order.statusHistory.map((h) => (
                  <div key={h.id} className="rounded-xl bg-white/70 p-3 ring-1 ring-black/5">
                    <div className="flex items-center justify-between gap-3">
                      <div className="text-sm font-medium text-ink-900">{h.newStatus}</div>
                      <div className="text-xs text-ink-700">
                        {new Date(h.changedAt).toLocaleTimeString()}
                      </div>
                    </div>
                    <div className="mt-1 text-xs text-ink-700">
                      de {h.previousStatus ?? '—'} via <span className="font-mono">{h.source}</span>
                    </div>
                  </div>
                ))
              ) : (
                <div className="text-sm text-ink-700">Sem histórico.</div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
