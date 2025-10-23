import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import api from '../services/api';

const Navigation: React.FC = () => {
  const navigate = useNavigate();
  const token = localStorage.getItem('jwt');

  const handleLogout = async () => {
    try {
      await api.logout();
    } catch (err) {
      console.warn('logout failed', err);
    } finally {
      localStorage.removeItem('jwt');
      navigate('/');
      window.location.reload();
    }
  };

  return (
    <nav className="navbar">
      <div className="nav-container">
        <Link to="/" className="nav-logo">
          TodoList
        </Link>
        <ul className="nav-menu">
          <li className="nav-item">
            <Link to="/" className="nav-link">
              首页
            </Link>
          </li>
          <li className="nav-item">
            <Link to="/todo/create" className="nav-link">
              添加事项
            </Link>
          </li>
          {token ? (
            <li className="nav-item">
              <button className="btn btn-link nav-link" onClick={handleLogout}>
                登出
              </button>
            </li>
          ) : (
            <li className="nav-item">
              <Link to="/login" className="nav-link">
                登录
              </Link>
            </li>
          )}
        </ul>
      </div>
    </nav>
  );
};

export default Navigation;