import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Home from './pages/Home';
import CreateTodo from './pages/CreateTodo';
import Login from './pages/Login';
import Navigation from './components/Navigation';
import ApiTester from './components/ApiTester';
import './App.css';

// 定义待办事项接口
interface TodoItem {
  id: string;
  title: string;
  content: string;
  done: boolean;
  dueTo: string;
}

const App: React.FC = () => {
  return (
    <Router>
      <div className="App">
        <Navigation />
        <div className="main-content">
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/todo/create" element={<CreateTodo />} />
            <Route path="/login" element={<Login />} />
            <Route path="/api-test" element={<ApiTester />} />
          </Routes>
        </div>
      </div>
    </Router>
  );
};

export default App;