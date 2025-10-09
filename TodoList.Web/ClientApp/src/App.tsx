import React, { useState, useEffect } from 'react';
import './App.css';

interface TodoItem {
  id: string;
  title: string;
  content: string;
  done: boolean;
  dueTo: string;
}

const App: React.FC = () => {
  const [todoItems, setTodoItems] = useState<TodoItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    // 模拟从后端获取数据
    fetchTodoItems();
  }, []);

  const fetchTodoItems = async () => {
    try {
      // 这里应该调用实际的API端点
      // 例如: const response = await fetch('/api/todoitems');
      // 暂时使用模拟数据
      const mockData: TodoItem[] = [
        {
          id: '1',
          title: '学习React',
          content: '学习如何在ASP.NET Core中集成React',
          done: false,
          dueTo: '2025-12-31T12:00:00Z'
        },
        {
          id: '2',
          title: '完成项目',
          content: '完成TodoList项目的React升级',
          done: false,
          dueTo: '2025-11-30T18:00:00Z'
        }
      ];
      
      setTodoItems(mockData);
      setLoading(false);
    } catch (error) {
      console.error('获取待办事项失败:', error);
      setLoading(false);
    }
  };

  const formatDueDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleString('zh-CN');
  };

  return (
    <div className="App">
      <header className="App-header text-center">
        <h1 className="display-4">我的待办事项 (React版本)</h1>
      </header>

      <div className="container">
        <div className="row">
          <div className="col-md-12">
            <div className="card">
              <div className="card-header">
                <h3>待办事项列表</h3>
                <button className="btn btn-primary float-right">添加新事项</button>
              </div>
              <div className="card-body">
                {loading ? (
                  <div className="alert alert-info">加载中...</div>
                ) : todoItems.length > 0 ? (
                  <div className="table-responsive">
                    <table className="table table-striped">
                      <thead>
                        <tr>
                          <th>标题</th>
                          <th>内容</th>
                          <th>截止日期</th>
                          <th>状态</th>
                          <th>操作</th>
                        </tr>
                      </thead>
                      <tbody>
                        {todoItems.map(item => (
                          <tr key={item.id}>
                            <td>{item.title}</td>
                            <td>{item.content}</td>
                            <td>{item.dueTo ? formatDueDate(item.dueTo) : '无截止日期'}</td>
                            <td>
                              {item.done ? (
                                <span className="badge badge-success" style={{ color: 'black' }}>已完成</span>
                              ) : (
                                <span className="badge badge-warning" style={{ color: 'black' }}>未完成</span>
                              )}
                            </td>
                            <td>
                              <button className="btn btn-sm btn-info mr-1">编辑</button>
                              <button className="btn btn-sm btn-danger">删除</button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <div className="alert alert-info">
                    <p>暂无待办事项，请添加新事项。</p>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default App;