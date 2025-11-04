import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../services/api';

const CreateTodo: React.FC = () => {
  const [title, setTitle] = useState<string>('');
  const [content, setContent] = useState<string>('');
  const [dueTo, setDueTo] = useState<string>('');
  const [tags, setTag] = useState<string>('');
  const navigate = useNavigate();

  const createTodoItem = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      // Normalize tags: split, trim and rejoin without spaces to satisfy server validation
      const sanitizedTags = tags
        .split(',')
        .map(t => t.trim())
        .filter(t => t.length > 0)
        .join(',');

      await api.createTodo({ title, content, duetoDateTime: dueTo ? new Date(dueTo).toISOString() : null, tags: sanitizedTags });
      navigate('/');
    } catch (error) {
      console.error('创建待办事项失败:', error);
    }
  };

  return (
    <div className="container">
      <h1>创建待办事项</h1>
      <hr />
      <div className="row">
        <div className="col-md-6">
          <form onSubmit={createTodoItem}>
            <div className="form-group mb-3">
              <label htmlFor="title" className="control-label">标题</label>
              <input
                type="text"
                className="form-control"
                id="title"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="请输入标题（1-50个字符）"
                required
                minLength={1}
                maxLength={50}
              />
              <div className="invalid-feedback">
                标题长度应在1-50个字符之间
              </div>
            </div>
            <div className="form-group mb-3">
              <label htmlFor="content" className="control-label">内容</label>
              <textarea
                className="form-control"
                id="content"
                value={content}
                onChange={(e) => setContent(e.target.value)}
                rows={4}
                placeholder="请输入内容（1-200个字符）"
                required
                minLength={1}
                maxLength={200}
              />
              <div className="invalid-feedback">
                内容长度应在1-200个字符之间
              </div>
            </div>
            <div className="form-group mb-3">
              <label htmlFor="dueTo" className="control-label">截止日期</label>
              <input
                type="datetime-local"
                className="form-control"
                id="dueTo"
                value={dueTo}
                onChange={(e) => setDueTo(e.target.value)}
              />
            </div>
            <div className="form-group mb-3">
              <label htmlFor="tags" className="control-label">标签</label>
              <input
                type="text"
                className="form-control"
                id="tags"
                value={tags}
                onChange={(e) => setTag(e.target.value)}
                placeholder="请输入标签"
                required
                minLength={1}
                maxLength={200}
              />
              <div className="invalid-feedback">
                标签长度应在1-200个字符之间
              </div>
            </div>
            <div className="form-group">
              <input type="submit" value="创建" className="btn btn-primary" />
              <a href="/" className="btn btn-secondary">返回首页</a>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};

export default CreateTodo;