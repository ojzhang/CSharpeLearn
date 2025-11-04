// import React, { useState, useEffect } from 'react';
import type { TodoItem } from '../types/TodoItem';

interface TodoListProps {
  items: TodoItem[];
  onDelete: (id: string) => void;
  onToggle: (id: string, done: boolean) => void;
}

const TodoList: React.FC<TodoListProps> = ({ items, onDelete, onToggle }) => {
  const formatDueDate = (due: any): string => {
    if (due === null || due === undefined || due === '') return '无截止日期';

    let date: Date | null = null;

    if (due instanceof Date) {
      date = due;
    } else if (typeof due === 'number') {
      // assume ms timestamp
      date = new Date(due);
    } else if (typeof due === 'string') {
      // Try parsing as ISO string first
      date = new Date(due);

      // If invalid, try to handle special cases
      if (isNaN(date.getTime())) {
        // Handle ISO format with timezone offset like "2025-10-16T00:00:00"
        if (due.includes('T')) {
          date = new Date(due + 'Z'); // Treat as UTC
        }

        // If still invalid, try normalize timezone like +0800 or +08:00
        if (isNaN(date.getTime())) {
          try {
            const normalized = due.replace(/([+-]\d{2}):?(\d{2})$/, '$1:$2');
            date = new Date(normalized);
          } catch (_) {
            // ignore
          }
        }
      }

      // If still invalid, try extract ms
      if (isNaN(date.getTime())) {
        const msMatch = due.match(/\d{10,}/);
        if (msMatch) {
          const ms = parseInt(msMatch[0], 10);
          date = ms < 1e12 ? new Date(ms * 1000) : new Date(ms);
        }
      }
    } else {
      // fallback: stringify and parse
      try {
        date = new Date(String(due));
      } catch (_) {
        date = null;
      }
    }

    if (!date || isNaN(date.getTime())) {
      console.log('无法解析的日期格式:', due);
      return String(due);
    }

    return date.toLocaleString('zh-CN');
  };

  return (
    <div className="table-container">
      <table className="data-table">
        <thead>
          <tr>
            <th>标题</th>
            <th>内容</th>
            <th>截止日期</th>
            <th>状态</th>
            <th>标签</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          {items.map(item => {
            // API may return the date in several shapes. In particular the server currently
            // exposes an ISO string under the `duetoDateTime` (camelCase) property while
            // older/front-end types expect `dueTo`.
            // Prefer `duetoDateTime` (ISO string) if present, otherwise try other fields.
            const anyItem = item as any;
            const dueVal = anyItem.duetoDateTime ?? anyItem.dueTo ?? anyItem.DueTo ?? anyItem.DueToDateTime;

            return (
              <tr key={item.id}>
                <td>{item.title}</td>
                <td>{item.content}</td>
                <td>{dueVal ? formatDueDate(dueVal) : '无截止日期'}</td>
                <td>
                  <button
                    className={`btn ${item.done ? 'btn-success' : 'btn-warning'}`}
                    onClick={() => onToggle(item.id, item.done)}
                  >
                    {item.done ? '已完成' : '未完成'}
                  </button>
                </td>
                <td>{item.tags}</td>
                <td>
                  <button
                    className="btn btn-danger"
                    onClick={() => onDelete(item.id)}
                  >
                    删除
                  </button>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
};

export default TodoList;