declare class ApiManager {
  constructor();
  checkApiStatus(): Promise<boolean>;
  startApi(): Promise<void>;
  waitForApiReady(maxAttempts?: number): Promise<boolean>;
  stopApi(): void;
  ensureApiRunning(): Promise<boolean>;
  getStatus(): {
    isRunning: boolean;
    port: number;
    processId: number | undefined;
  };
}

export default ApiManager;
