import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../services/api';

const Login: React.FC = () => {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState<string | null>(null);
    const navigate = useNavigate();

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        try {
            await api.login(email, password);
            navigate('/');
            window.location.reload();
        } catch (err: any) {
            setError(err?.message || '登录失败');
        }
    };

    return (
        <div className="container">
            <h1>登录</h1>
            <form onSubmit={handleSubmit}>
                {error && <div className="alert alert-danger">{error}</div>}
                <div className="form-group mb-3">
                    <label htmlFor="email">邮箱</label>
                    <input id="email" type="email" className="form-control" value={email} onChange={e => setEmail(e.target.value)} required />
                </div>
                <div className="form-group mb-3">
                    <label htmlFor="password">密码</label>
                    <input id="password" type="password" className="form-control" value={password} onChange={e => setPassword(e.target.value)} required />
                </div>
                <button className="btn btn-primary" type="submit">登录</button>
            </form>
        </div>
    );
};

export default Login;
