import { useState, useEffect, useCallback } from 'react';
import { localStorageService } from '../services/LocalStorageService';

export interface LocalNode {
  id: string;
  machineName: string;
  operatingSystem: string;
  systemVersion: string;
  architecture: string;
  ipAddress?: string;
  port: number;
  isActive: boolean;
  isAnonymous: boolean;
  userId?: string;
  createdAt: string;
  updatedAt: string;
  lastSeen: string;
}

export interface UseLocalNodeReturn {
  localNode: LocalNode | null;
  isLoading: boolean;
  error: string | null;
  saveLocalNode: (nodeData: {
    machineName: string;
    operatingSystem?: string;
    systemVersion?: string;
    architecture?: string;
    ipAddress?: string;
    port?: number;
  }) => Promise<boolean>;
  getLocalNodeByMachine: (machineName: string) => Promise<LocalNode | null>;
  getAllLocalNodes: () => Promise<LocalNode[]>;
  hasLocalNode: (machineName: string) => Promise<boolean>;
  getSystemInfoAndSaveNode: () => Promise<LocalNode | null>;
  refreshLocalNode: () => Promise<void>;
}

export const useLocalNode = (): UseLocalNodeReturn => {
  const [localNode, setLocalNode] = useState<LocalNode | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const saveLocalNode = useCallback(async (nodeData: {
    machineName: string;
    operatingSystem?: string;
    systemVersion?: string;
    architecture?: string;
    ipAddress?: string;
    port?: number;
  }): Promise<boolean> => {
    setIsLoading(true);
    setError(null);

    try {
      const result = await localStorageService.saveLocalNode(nodeData);
      
      if (result.success && result.data) {
        setLocalNode(result.data as unknown as LocalNode);
        return true;
      } else {
        setError(result.message || 'Erro ao salvar nó local');
        return false;
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Erro desconhecido';
      setError(errorMessage);
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);

  const getLocalNodeByMachine = useCallback(async (machineName: string): Promise<LocalNode | null> => {
    setIsLoading(true);
    setError(null);

    try {
      const result = await localStorageService.getLocalNodeByMachine(machineName);
      
      if (result.success && result.data) {
        setLocalNode(result.data as unknown as LocalNode);
        return result.data as unknown as LocalNode;
      } else {
        setError(result.message || 'Nó local não encontrado');
        return null;
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Erro desconhecido';
      setError(errorMessage);
      return null;
    } finally {
      setIsLoading(false);
    }
  }, []);

  const getAllLocalNodes = useCallback(async (): Promise<LocalNode[]> => {
    setIsLoading(true);
    setError(null);

    try {
      const result = await localStorageService.getAllLocalNodes();
      
      if (result.success && result.data) {
        return result.data as unknown as LocalNode[];
      } else {
        setError(result.message || 'Erro ao obter nós locais');
        return [];
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Erro desconhecido';
      setError(errorMessage);
      return [];
    } finally {
      setIsLoading(false);
    }
  }, []);

  const hasLocalNode = useCallback(async (machineName: string): Promise<boolean> => {
    try {
      const result = await localStorageService.hasLocalNode(machineName);
      return result.success && result.data?.exists === true;
    } catch (err) {
      console.error('Erro ao verificar existência do nó local:', err);
      return false;
    }
  }, []);

  const getSystemInfoAndSaveNode = useCallback(async (): Promise<LocalNode | null> => {
    setIsLoading(true);
    setError(null);

    try {
      const result = await localStorageService.getSystemInfoAndSaveNode();
      
      if (result.success && result.data) {
        setLocalNode(result.data as unknown as LocalNode);
        return result.data as unknown as LocalNode;
      } else {
        setError(result.message || 'Erro ao obter informações do sistema e salvar nó');
        return null;
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Erro desconhecido';
      setError(errorMessage);
      return null;
    } finally {
      setIsLoading(false);
    }
  }, []);

  const refreshLocalNode = useCallback(async (): Promise<void> => {
    if (localNode?.machineName) {
      await getLocalNodeByMachine(localNode.machineName);
    }
  }, [localNode?.machineName, getLocalNodeByMachine]);

  // Auto-inicializar nó local na montagem do componente
  useEffect(() => {
    const initializeLocalNode = async () => {
      try {
        // Usar o handler IPC para obter informações do sistema
        if (window.system && 'getSystemInfo' in window.system) {
          const systemInfo = await (window.system as any).getSystemInfo();
          
          if (systemInfo.success && systemInfo.data) {
            const { machineName, operatingSystem, systemVersion, architecture } = systemInfo.data;
            
            // Garantir que existe um nó local (método unificado)
            console.log('🔍 Garantindo existência do nó local para:', machineName);
            const result = await localStorageService.getSystemInfoAndSaveNode();
            
            if (result.success && result.data) {
              setLocalNode(result.data as LocalNode);
              console.log('✅ Nó local garantido:', result.data);
            } else {
              console.error('❌ Erro ao garantir nó local:', result.message);
            }
          }
        }
      } catch (err) {
        console.error('Erro ao inicializar nó local:', err);
      }
    };

    initializeLocalNode();
  }, [hasLocalNode, getLocalNodeByMachine, saveLocalNode]);

  return {
    localNode,
    isLoading,
    error,
    saveLocalNode,
    getLocalNodeByMachine,
    getAllLocalNodes,
    hasLocalNode,
    getSystemInfoAndSaveNode,
    refreshLocalNode,
  };
};
