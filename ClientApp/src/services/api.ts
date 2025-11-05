const API_BASE = "http://localhost:5059";

type LoginResponse = { token: string };

export async function login(email: string, password: string) {
    const res = await fetch(`${API_BASE}/api/accounts/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
    });
    if (!res.ok) throw new Error(res.statusText);
    const data = (await res.json()) as LoginResponse;
    localStorage.setItem('jwt', data.token);
    return data.token;
}

export function getToken() {
    return localStorage.getItem('jwt');
}

function authHeaders(): Record<string, string> {
    const token = getToken();
    return token ? { Authorization: `Bearer ${token}` } : ({} as Record<string, string>);
}

export async function getTodos() {
    const res = await fetch(`${API_BASE}/api/todoitems/getitems`, {
        headers: authHeaders(),
    });
    if (!res.ok) throw new Error(res.statusText);
    return res.json();
}

export async function createTodo(todo: any) {
    const res = await fetch(`${API_BASE}/api/todoitems`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', ...authHeaders() } as Record<string, string>,
        body: JSON.stringify(todo),
    });
    if (!res.ok) throw new Error(res.statusText);
    return res.json();
}

export async function deleteTodoById(id: string) {
    const res = await fetch(`${API_BASE}/api/todoitems/${id}`, {
        method: 'DELETE',
        headers: authHeaders(),
    });
    if (!res.ok) throw new Error(res.statusText);
    return res.ok;
}

export async function toggleTodoItem(id: string, done: boolean) {
    const res = await fetch(`${API_BASE}/api/todoitems/${id}`, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${localStorage.getItem('jwt')}`
        },
        body: JSON.stringify({
            done: !done,
        }),
    });
    if (!res.ok) throw new Error(res.statusText);
    return res;
}

export async function uploadFile(todoId: string, file: File) {
    const fd = new FormData();
    fd.append('file', file);
    const res = await fetch(`${API_BASE}/api/todoitems/${todoId}`, {
        method: 'POST',
        body: fd,
        headers: authHeaders(),
    });
    if (!res.ok) throw new Error(res.statusText);
    return res.json();
}

export async function logout() {
    const res = await fetch(`${API_BASE}/api/accounts/logout`, {
        method: 'POST',
        headers: authHeaders(),
    });
    if (!res.ok) throw new Error(res.statusText);
    localStorage.removeItem('jwt');
}

export default { login, getTodos, createTodo, uploadFile, logout, deleteTodoById, toggleTodoItem };
