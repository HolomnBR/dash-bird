// Script para testar a API local
const fetch = require('node-fetch');

const API_URL = 'http://localhost:5000';

async function testAPI() {
  console.log('🧪 Testando API local...\n');

  // Teste 1: Health check
  try {
    console.log('1. Testando health check...');
    const healthResponse = await fetch(`${API_URL}/api/Health`);
    console.log(`   Status: ${healthResponse.status}`);
    if (healthResponse.ok) {
      const healthData = await healthResponse.text();
      console.log(`   Resposta: ${healthData}`);
    }
  } catch (error) {
    console.log(`   ❌ Erro: ${error.message}`);
  }

  // Teste 2: Registro de usuário
  try {
    console.log('\n2. Testando registro de usuário...');
    const registerData = {
      name: 'Teste Usuário',
      email: 'teste@exemplo.com',
      password: '123456'
    };

    const registerResponse = await fetch(`${API_URL}/api/Auth/register`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(registerData)
    });

    console.log(`   Status: ${registerResponse.status}`);
    const registerText = await registerResponse.text();
    console.log(`   Resposta: ${registerText}`);

    if (registerResponse.ok) {
      const registerResult = JSON.parse(registerText);
      console.log(`   ✅ Usuário registrado com sucesso!`);
      console.log(`   Token: ${registerResult.token?.substring(0, 20)}...`);
      
      // Teste 3: Login
      console.log('\n3. Testando login...');
      const loginData = {
        email: 'teste@exemplo.com',
        password: '123456'
      };

      const loginResponse = await fetch(`${API_URL}/api/Auth/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(loginData)
      });

      console.log(`   Status: ${loginResponse.status}`);
      const loginText = await loginResponse.text();
      console.log(`   Resposta: ${loginText}`);

      if (loginResponse.ok) {
        const loginResult = JSON.parse(loginText);
        console.log(`   ✅ Login realizado com sucesso!`);
        console.log(`   Token: ${loginResult.token?.substring(0, 20)}...`);
        
        // Teste 4: Perfil
        console.log('\n4. Testando obtenção de perfil...');
        const profileResponse = await fetch(`${API_URL}/api/Auth/profile`, {
          method: 'GET',
          headers: {
            'Authorization': `Bearer ${loginResult.token}`,
          }
        });

        console.log(`   Status: ${profileResponse.status}`);
        const profileText = await profileResponse.text();
        console.log(`   Resposta: ${profileText}`);
      }
    }
  } catch (error) {
    console.log(`   ❌ Erro: ${error.message}`);
  }

  console.log('\n✅ Testes concluídos!');
}

testAPI().catch(console.error);
