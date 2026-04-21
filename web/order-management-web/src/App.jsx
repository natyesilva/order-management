import { Navigate, Route, Routes } from 'react-router-dom'
import Layout from './components/Layout.jsx'
import OrdersListPage from './pages/OrdersListPage.jsx'
import OrderCreatePage from './pages/OrderCreatePage.jsx'
import OrderDetailPage from './pages/OrderDetailPage.jsx'

export default function App() {
  return (
    <Layout>
      <Routes>
        <Route path="/" element={<Navigate to="/orders" replace />} />
        <Route path="/orders" element={<OrdersListPage />} />
        <Route path="/orders/new" element={<OrderCreatePage />} />
        <Route path="/orders/:id" element={<OrderDetailPage />} />
        <Route path="*" element={<Navigate to="/orders" replace />} />
      </Routes>
    </Layout>
  )
}

