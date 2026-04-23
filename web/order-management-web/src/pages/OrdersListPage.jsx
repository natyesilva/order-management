import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import StatusBadge from '../components/StatusBadge.jsx'
import { listOrders } from '../lib/api.js'

function money(value) {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' }).format(value)
}

export default function OrdersListPage() {
  const [orders, setOrders] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const pollingMs = useMemo(() => {
    const raw = import.meta.env.VITE_POLLING_MS
    const n = raw ? Number(raw) : 2000
    return Number.isFinite(n) && n > 250 ? n : 2000
  }, [])

  async function load() {
    try {
      setError(null)
      const data = await listOrders()
      setOrders(data)
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
  }, [pollingMs])

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-ink-900">Pedidos</h1>
          <p className="mt-1 text-sm text-ink-700">
            As atualizações de status são aplicadas de forma assíncrona pelo worker.
          </p>
        </div>
        <Link
          to="/orders/new"
          className="inline-flex items-center justify-center rounded-xl bg-ink-900 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-ink-700"
        >
          Criar pedido
        </Link>
      </div>

      <div className="rounded-2xl border border-black/5 bg-white/70 shadow-sm ring-1 ring-black/5">
        <div className="overflow-x-auto">
          <table className="min-w-full text-left text-sm">
            <thead className="border-b border-black/5 bg-white/50">
              <tr className="text-ink-700">
                <th className="px-4 py-3 font-medium">Cliente</th>
                <th className="px-4 py-3 font-medium">Produto</th>
                <th className="px-4 py-3 font-medium">Valor</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 font-medium">Criado em</th>
                <th className="px-4 py-3 font-medium"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-black/5">
              {loading ? (
                <tr>
                  <td className="px-4 py-6 text-ink-700" colSpan={6}>
                    Carregando...
                  </td>
                </tr>
              ) : error ? (
                <tr>
                  <td className="px-4 py-6 text-red-700" colSpan={6}>
                    {error}
                  </td>
                </tr>
              ) : orders.length === 0 ? (
                <tr>
                  <td className="px-4 py-6 text-ink-700" colSpan={6}>
                    Nenhum pedido ainda.
                  </td>
                </tr>
              ) : (
                orders.map((o) => (
                  <tr key={o.id} className="hover:bg-white/60">
                    <td className="px-4 py-3 font-medium text-ink-900">{o.customer}</td>
                    <td className="px-4 py-3 text-ink-700">{o.product}</td>
                    <td className="px-4 py-3 text-ink-900">{money(o.value)}</td>
                    <td className="px-4 py-3">
                      <StatusBadge status={o.status} />
                    </td>
                    <td className="px-4 py-3 text-ink-700">
                      {new Date(o.createdAt).toLocaleString()}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <Link
                        to={`/orders/${o.id}`}
                        className="rounded-lg px-3 py-1.5 text-sm font-medium text-ink-900 hover:bg-black/5"
                      >
                        Detalhes
                      </Link>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
