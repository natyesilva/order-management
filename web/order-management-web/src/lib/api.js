const baseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080'

function getLocale() {
  try {
    return localStorage.getItem('om.locale') || 'pt-BR'
  } catch {
    return 'pt-BR'
  }
}

async function http(path, options) {
  const res = await fetch(`${baseUrl}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(options?.headers || {}),
    },
    ...options,
  })

  if (!res.ok) {
    let body = null
    try {
      body = await res.json()
    } catch {
      // ignore
    }
    const locale = getLocale()
    const fallback = locale === 'en' ? `Request failed: ${res.status}` : `Falha na requisição: ${res.status}`
    const msg = body?.title || body?.detail || fallback
    const err = new Error(msg)
    err.status = res.status
    err.body = body
    throw err
  }

  if (res.status === 204) return null
  return res.json()
}

export function listOrders() {
  return http('/orders')
}

export function getOrder(id) {
  return http(`/orders/${id}`)
}

export function createOrder(payload) {
  return http('/orders', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

