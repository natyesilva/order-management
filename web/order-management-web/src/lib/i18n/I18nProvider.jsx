import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { messages } from './messages.js'

const STORAGE_KEY = 'om.locale'

const I18nContext = createContext(null)

function normalizeLocale(locale) {
  if (locale === 'pt-BR') return 'pt-BR'
  if (locale === 'en') return 'en'
  return 'pt-BR'
}

export function I18nProvider({ children }) {
  const [locale, setLocale] = useState('pt-BR')

  useEffect(() => {
    const stored = localStorage.getItem(STORAGE_KEY)
    if (stored) setLocale(normalizeLocale(stored))
  }, [])

  const setAppLocale = useCallback((next) => {
    const normalized = normalizeLocale(next)
    setLocale(normalized)
    localStorage.setItem(STORAGE_KEY, normalized)
    document.documentElement.lang = normalized === 'pt-BR' ? 'pt-BR' : 'en'
  }, [])

  const dict = messages[locale] || messages['pt-BR']

  const t = useCallback(
    (key) => {
      const v = dict[key]
      return v ?? key
    },
    [dict],
  )

  const value = useMemo(() => ({ locale, setLocale: setAppLocale, t }), [locale, setAppLocale, t])

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>
}

export function useI18n() {
  const ctx = useContext(I18nContext)
  if (!ctx) throw new Error('useI18n must be used within I18nProvider')
  return ctx
}

