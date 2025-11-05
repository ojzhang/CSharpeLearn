import React, { useState, useEffect } from 'react';
import { TodoItem } from '../types/TodoItem';
import TodoList from '../components/TodoList';
import api from '../services/api';

const Home: React.FC = () => {
  const [todoItems, setTodoItems] = useState<TodoItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    fetchTodoItems();
  }, []);

  const fetchTodoItems = async () => {
    try {
      // 使用 api helper 获取待办事项（会自动带上 JWT）
      const data = await api.getTodos();
      console.log('Todo items data:', data);
      setTodoItems(data);
    } catch (error) {
      console.error('获取待办事项失败:', error);
    } finally {
      setLoading(false);
    }
  };

  const deleteTodoItem = async (id: string) => {
    try {
      await api.deleteTodoById(id);
      // 删除成功后重新获取待办事项列表
      fetchTodoItems();
    } catch (error) {
      console.error('删除待办事项失败:', error);
    }
  };

  const toggleTodoItem = async (id: string, done: boolean) => {
    try {
      const response = await api.toggleTodoItem(id, done);
      if (response.ok) {
        // 重新获取待办事项列表
        fetchTodoItems();
      } else {
        console.error('更新待办事项失败:', response.statusText);
      }
    } catch (error) {
      console.error('更新待办事项失败:', error);
    }
  };

  return (
    <div className="container">
      <div className="text-center">
        <h1 className="display-4">欢迎使用待办事项</h1>
      </div>

      <div className="row">
        <div className="col-md-12">
          <div className="card">
            <div className="card-header">
              <h3>待办事项列表</h3>
              <a href="/todo/create" className="btn btn-primary float-right">
                添加新事项
              </a>
            </div>
            <div className="card-body">
              {loading ? (
                <div className="alert info">加载中...</div>
              ) : todoItems.length > 0 ? (
                <TodoList
                  items={todoItems}
                  onDelete={deleteTodoItem}
                  onToggle={toggleTodoItem}
                />
              ) : (
                <div className="alert info">
                  <p>暂无待办事项，请添加新事项。</p>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Home;