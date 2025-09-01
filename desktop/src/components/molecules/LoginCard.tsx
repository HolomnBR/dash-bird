import React, { useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuthContext } from '../../contexts/AuthContext'
import { Button } from '../atoms/Button'

export const LoginCard: React.FC = () => {
  const { isAuthenticated, user, login, logout } = useAuthContext()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError(null)

    try {
      const result = await login(email, password)
      if (!result.success) {
        setError(result.error || 'Erro ao fazer login')
      }
    } catch (err) {
      setError('Erro de conexão. Verifique se a API local está rodando.')
    } finally {
      setLoading(false)
    }
  }

  const handleLogout = async () => {
    await logout()
    // Limpar campos do formulário após logout
    setEmail('')
    setPassword('')
    setError(null)
  }

  if (isAuthenticated && user) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Conta Conectada</h3>
        
        <div className="flex items-center space-x-3 mb-4">
          <div className="w-10 h-10 bg-blue-500 rounded-full flex items-center justify-center text-white font-medium">
            {user.name.charAt(0).toUpperCase()}
          </div>
          <div>
            <div className="font-medium text-gray-900">{user.name}</div>
            <div className="text-sm text-gray-500">{user.email}</div>
          </div>
        </div>

        <div className="text-sm text-gray-600 mb-4">
          <div>Conta criada em: {new Date(user.createdAt).toLocaleDateString('pt-BR')}</div>
          {user.lastLoginAt && (
            <div>Último login: {new Date(user.lastLoginAt).toLocaleDateString('pt-BR')}</div>
          )}
        </div>

        <Button
          onClick={handleLogout}
          className="w-full bg-red-600 hover:bg-red-700 text-white"
        >
          Sair da Conta
        </Button>
      </div>
    )
  }

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <h3 className="text-lg font-semibold text-gray-900 mb-4">Fazer Login</h3>
      
      {error && (
        <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      <form onSubmit={handleLogin} className="space-y-4">
        <div>
          <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
            Email
          </label>
          <input
            type="email"
            id="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            placeholder="seu@email.com"
          />
        </div>

        <div>
          <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
            Senha
          </label>
          <input
            type="password"
            id="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            placeholder="Sua senha"
          />
        </div>

        <Button
          type="submit"
          disabled={loading}
          className="w-full bg-blue-600 hover:bg-blue-700 text-white"
        >
          {loading ? 'Entrando...' : 'Entrar'}
        </Button>
      </form>

      <div className="mt-4 text-center">
        <Link
          to="/register"
          className="text-blue-600 hover:text-blue-800 text-sm font-medium"
        >
          Não tem uma conta? Registre-se
        </Link>
      </div>

      <div className="mt-4 text-xs text-gray-500 text-center">
        <p>💡 Conecte-se para sincronizar seus dados entre dispositivos.</p>
      </div>
    </div>
  )
}
