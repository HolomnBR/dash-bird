import React, { useEffect, useState } from 'react';
import { useLocalNode } from '../hooks/useLocalNode';
import { useAuthContext } from '../contexts/AuthContext';
import type { LocalNode } from '../hooks/useLocalNode';
import { Settings } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

interface LocalNodeInfoProps {
  className?: string;
  showDetails?: boolean;
}

export const LocalNodeInfo: React.FC<LocalNodeInfoProps> = ({ 
  className = '', 
  showDetails = true 
}) => {
  const { localNode, isLoading, error } = useLocalNode();
  const { isAuthenticated, token } = useAuthContext();
  const navigate = useNavigate();
  
  // Estado para informações atualizadas do servidor
  const [serverNodeInfo, setServerNodeInfo] = useState<{
    isConnected: boolean;
    isAnonymous: boolean;
    userId?: string;
  } | null>(null);
  const [checkingServerStatus, setCheckingServerStatus] = useState(false);
  const [nodeConfig, setNodeConfig] = useState<{
    nodeId: string;
    machineId: string;
    machineName: string;
    alias?: string;
    createdAt: string;
  } | null>(null);

  // Função para carregar configuração do nó
  const loadNodeConfig = async () => {
    try {
      if (window.system) {
        const cfg = await window.system.getNodeConfig();
        setNodeConfig(cfg);
      }
    } catch (error) {
      console.error('Erro ao carregar configuração do nó:', error);
    }
  };

  // Função para verificar status do nó no servidor
  const checkServerNodeStatus = async () => {
    if (!nodeConfig || !isAuthenticated || !token) return;
    
    setCheckingServerStatus(true);
    try {
      const result = await window.auth.checkNodeConnection({
        token,
        machineId: nodeConfig.machineId
      });
      
      if (result.success) {
        setServerNodeInfo({
          isConnected: result.isConnected || false,
          isAnonymous: !result.isConnected, // Se não está conectado, é anônimo
          userId: result.node?.id
        });
      }
    } catch (error) {
      console.error('Erro ao verificar status do servidor:', error);
    } finally {
      setCheckingServerStatus(false);
    }
  };

  // Carregar configuração do nó quando o componente monta
  useEffect(() => {
    loadNodeConfig();
  }, []);

  // Verificar status do servidor quando o componente monta ou quando o usuário faz login
  useEffect(() => {
    if (nodeConfig && isAuthenticated && token) {
      checkServerNodeStatus();
    } else if (!isAuthenticated) {
      // Se não está autenticado, limpar informações do servidor
      setServerNodeInfo(null);
    }
  }, [nodeConfig, isAuthenticated, token]);

  // Determinar se o nó é anônimo baseado nas informações do servidor (se disponível) ou locais
  const isNodeAnonymous = serverNodeInfo ? serverNodeInfo.isAnonymous : localNode?.isAnonymous ?? true;

  if (isLoading) {
    return (
      <div 
        className={`p-4 bg-gray-100 rounded-lg cursor-pointer hover:shadow-md transition-shadow ${className}`}
        onClick={() => navigate('/settings')}
      >
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
            <span className="text-sm text-gray-600">Carregando informações do nó local...</span>
          </div>
          <button
            onClick={(e) => {
              e.stopPropagation();
              navigate('/settings');
            }}
            className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-200 rounded-lg transition-colors"
            title="Configurações"
          >
            <Settings size={20} />
          </button>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div 
        className={`p-4 bg-red-50 border border-red-200 rounded-lg cursor-pointer hover:shadow-md transition-shadow ${className}`}
        onClick={() => navigate('/settings')}
      >
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-sm font-medium text-red-800">Erro ao carregar nó local</h3>
            <p className="text-sm text-red-600 mt-1">{error}</p>
          </div>
          <button
            onClick={(e) => {
              e.stopPropagation();
              navigate('/settings');
            }}
            className="p-2 text-gray-400 hover:text-gray-600 hover:bg-red-100 rounded-lg transition-colors"
            title="Configurações"
          >
            <Settings size={20} />
          </button>
        </div>
      </div>
    );
  }

  if (!localNode) {
    return (
      <div className={`p-4 bg-yellow-50 border border-yellow-200 rounded-lg ${className}`}>
        <div>
          <h3 className="text-sm font-medium text-yellow-800">Nó local não encontrado</h3>
          <p className="text-sm text-yellow-600 mt-1">Nenhum nó local foi registrado ainda.</p>
        </div>
      </div>
    );
  }

  return (
    <div 
      className={`p-4 bg-white border border-gray-200 rounded-lg shadow-sm cursor-pointer hover:shadow-md transition-shadow ${className}`}
      onClick={() => navigate('/settings')}
    >
      <div className="flex items-center justify-between mb-3">
        <h3 className="text-lg font-semibold text-gray-900">Nó Local</h3>
        <div className="flex items-center space-x-2">
          <div className={`w-2 h-2 rounded-full ${localNode.isActive ? 'bg-green-500' : 'bg-red-500'}`}></div>
          <span className="text-xs text-gray-500">
            {localNode.isActive ? 'Ativo' : 'Inativo'}
          </span>
          <button
            onClick={(e) => {
              e.stopPropagation();
              navigate('/settings');
            }}
            className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg transition-colors ml-2"
            title="Configurações"
          >
            <Settings size={20} />
          </button>
        </div>
      </div>

      <div className="space-y-2">
        <div className="flex justify-between items-center">
          <span className="text-sm font-medium text-gray-600">Máquina:</span>
          <span className="text-sm text-gray-900 font-mono">{localNode.machineName}</span>
        </div>

        <div className="flex justify-between items-center">
          <span className="text-sm font-medium text-gray-600">Sistema:</span>
          <span className="text-sm text-gray-900">{localNode.operatingSystem}</span>
        </div>

        <div className="flex justify-between items-center">
          <span className="text-sm font-medium text-gray-600">Versão:</span>
          <span className="text-sm text-gray-900">{localNode.systemVersion}</span>
        </div>

        <div className="flex justify-between items-center">
          <span className="text-sm font-medium text-gray-600">Arquitetura:</span>
          <span className="text-sm text-gray-900">{localNode.architecture}</span>
        </div>

        {showDetails && (
          <>
            <div className="flex justify-between items-center">
              <span className="text-sm font-medium text-gray-600">IP:</span>
              <span className="text-sm text-gray-900 font-mono">
                {localNode.ipAddress || 'N/A'}
              </span>
            </div>

            <div className="flex justify-between items-center">
              <span className="text-sm font-medium text-gray-600">Porta:</span>
              <span className="text-sm text-gray-900">{localNode.port}</span>
            </div>

            <div className="flex justify-between items-center">
              <span className="text-sm font-medium text-gray-600">Tipo:</span>
              <span className="text-sm text-gray-900">
                {isNodeAnonymous ? 'Anônimo' : 'Autenticado'}
                {checkingServerStatus && (
                  <span className="ml-2 text-xs text-blue-600">🔄 Verificando...</span>
                )}
              </span>
            </div>


            <div className="flex justify-between items-center">
              <span className="text-sm font-medium text-gray-600">Última vez visto:</span>
              <span className="text-sm text-gray-900">
                {new Date(localNode.lastSeen).toLocaleString('pt-BR')}
              </span>
            </div>

            <div className="flex justify-between items-center">
              <span className="text-sm font-medium text-gray-600">Criado em:</span>
              <span className="text-sm text-gray-900">
                {new Date(localNode.createdAt).toLocaleString('pt-BR')}
              </span>
            </div>
            
            {/* Botão para atualizar status do servidor */}
            {isAuthenticated && (
              <div className="flex justify-center pt-2">
                <button
                  onClick={checkServerNodeStatus}
                  disabled={checkingServerStatus}
                  className="px-3 py-1 text-xs bg-blue-100 text-blue-700 rounded hover:bg-blue-200 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1"
                >
                  {checkingServerStatus ? (
                    <>
                      <div className="w-3 h-3 border-2 border-blue-700 border-t-transparent rounded-full animate-spin"></div>
                      Verificando...
                    </>
                  ) : (
                    <>
                      🔄 Atualizar Status
                    </>
                  )}
                </button>
              </div>
            )}
          </>
        )}
      </div>

    </div>
  );
};

export default LocalNodeInfo;
