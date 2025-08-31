import http from 'http';

console.log('🧪 Testando API...');

const testApi = () => {
  return new Promise((resolve) => {
    const req = http.request({
      hostname: 'localhost',
      port: 5000,
      path: '/health',
      method: 'GET',
      timeout: 5000
    }, (res) => {
      let data = '';
      res.on('data', (chunk) => {
        data += chunk;
      });
      res.on('end', () => {
        try {
          const response = JSON.parse(data);
          console.log('✅ API está funcionando!');
          console.log('📊 Status:', response);
          resolve(true);
        } catch (error) {
          console.log('⚠️ API respondeu mas com formato inesperado');
          resolve(false);
        }
      });
    });

    req.on('error', (error) => {
      console.log('❌ Erro ao conectar com a API:', error.message);
      resolve(false);
    });

    req.on('timeout', () => {
      console.log('⏰ Timeout ao conectar com a API');
      req.destroy();
      resolve(false);
    });

    req.end();
  });
};

// Testar a API
testApi().then((isWorking) => {
  if (isWorking) {
    console.log('🎉 Teste concluído com sucesso!');
    process.exit(0);
  } else {
    console.log('💥 Teste falhou!');
    process.exit(1);
  }
});
