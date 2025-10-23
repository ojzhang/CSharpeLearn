import React, { useState } from 'react';
import * as api from '../services/api';

const ApiTester: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [result, setResult] = useState<any>(null);
  const [todoTitle, setTodoTitle] = useState('');
  const [todos, setTodos] = useState<any[]>([]);


  const handleLogin = async () => {
    try {
      const token = await api.login(email, password);
      setResult({ success: true, data: { token } });
    } catch (error) {
      setResult({ success: false, error: (error as Error).message });
    }
  };

  const handleGetTodos = async () => {
    try {
      const data = await api.getTodos();
      setTodos(data);
      setResult({ success: true, data });
    } catch (error) {
      setResult({ success: false, error: (error as Error).message });
    }
  };

  const handleCreateTodo = async () => {
    try {
      const newTodo = {
        title: todoTitle,
        content: 'Test content',
        done: false,
        duetoDateTime: new Date().toISOString()
      };
      const data = await api.createTodo(newTodo);
      setResult({ success: true, data });
    } catch (error) {
      setResult({ success: false, error: (error as Error).message });
    }
  };

  const handleLogout = async () => {
    try {
      await api.logout();
      setResult({ success: true, message: 'Logged out successfully' });
    } catch (error) {
      setResult({ success: false, error: (error as Error).message });
    }
  };

  return (
    <div style={{ padding: '20px' }}>
      <h2>API测试工具</h2>

      <div style={{ marginBottom: '20px' }}>
        <h3>登录测试</h3>
        <input
          type="text"
          placeholder="邮箱"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />
        <input
          type="password"
          placeholder="密码"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
        <button onClick={handleLogin}>登录</button>
      </div>

      <div style={{ marginBottom: '20px' }}>
        <h3>待办事项操作</h3>
        <button onClick={handleGetTodos}>获取待办事项</button>
        <input
          type="text"
          placeholder="待办事项标题"
          value={todoTitle}
          onChange={(e) => setTodoTitle(e.target.value)}
        />
        <button onClick={handleCreateTodo}>创建待办事项</button>
        <button onClick={handleLogout}>登出</button>
      </div>

      <div>
        <h3>测试结果</h3>
        <pre>{JSON.stringify(result, null, 2)}</pre>
      </div>

      {todos.length > 0 && (
        <div>
          <h3>待办事项列表</h3>
          <ul>
            {todos.map((todo: any) => (
              <li key={todo.id}>{todo.title}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
};

export default ApiTester;