import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuthContext } from '../contexts/AuthContext'
import { Button } from '../components/atoms/Button'
import { Header } from '../components/molecules/Header'
import { ArrowLeft } from 'lucide-react'

export function Register() {
  const { register } = useAuthContext()
  const navigate = useNavigate()
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    password: '',
    confirmPassword: ''
  })
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target
    setFormData(prev => ({
      ...prev,
      [name]: value
    }))
    setError(null)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError(null)

    // Validações
    if (formData.password !== formData.confirmPassword) {
      setError('As senhas não coincidem')
      setLoading(false)
      return
    }

    if (formData.password.length < 6) {
      setError('A senha deve ter pelo menos 6 caracteres')
      setLoading(false)
      return
    }

    try {
      const result = await register(formData.name, formData.email, formData.password)
      if (result.success) {
        // Redirecionar para settings após registro bem-sucedido (já logado)
        navigate('/settings', { 
          state: { 
            message: 'Conta criada com sucesso! Você já está logado.',
            showLogin: false 
          }
        })
      } else {
        setError(result.error || 'Erro ao criar conta')
      }
    } catch (err) {
      setError('Erro de conexão. Verifique se a API local está rodando.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="mx-auto max-w-md">
      <Header
        leftSlot={
          <Link to="/settings" aria-label="Voltar" title="Voltar">
            <ArrowLeft size={18} className="opacity-80 hover:opacity-100" />
          </Link>
        }
        title="Criar Conta"
        subtitle="Registre-se para sincronizar seus dados"
      />
      
      <div className="p-6">
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          {error && (
            <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
                Nome Completo
              </label>
              <input
                type="text"
                id="name"
                name="name"
                value={formData.name}
                onChange={handleInputChange}
                required
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Seu nome completo"
              />
            </div>

            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                Email
              </label>
              <input
                type="email"
                id="email"
                name="email"
                value={formData.email}
                onChange={handleInputChange}
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
                name="password"
                value={formData.password}
                onChange={handleInputChange}
                required
                minLength={6}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Mínimo 6 caracteres"
              />
            </div>

            <div>
              <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-700 mb-1">
                Confirmar Senha
              </label>
              <input
                type="password"
                id="confirmPassword"
                name="confirmPassword"
                value={formData.confirmPassword}
                onChange={handleInputChange}
                required
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Confirme sua senha"
              />
            </div>

            <Button
              type="submit"
              disabled={loading}
              className="w-full bg-blue-600 hover:bg-blue-700 text-white"
            >
              {loading ? 'Criando Conta...' : 'Criar Conta'}
            </Button>
          </form>

          <div className="mt-6 text-center">
            <Link
              to="/settings"
              className="text-blue-600 hover:text-blue-800 text-sm"
            >
              Já tem uma conta? Faça login
            </Link>
          </div>

          <div className="mt-4 text-xs text-gray-500 text-center">
            <p>💡 Ao criar uma conta, você poderá sincronizar seus dados entre dispositivos.</p>
          </div>
        </div>
      </div>
    </main>
  )
}
