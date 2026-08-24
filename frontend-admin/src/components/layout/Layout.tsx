import React from 'react';
import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { Header } from './Header';

const Layout: React.FC = () => {
  return (
    <div className="flex h-screen w-screen overflow-hidden bg-slate-50/50 font-sans">
      {/* 1. SIDEBAR: Cố định bên trái, tự động co giãn theo state bên trong */}
      <Sidebar />

      {/* 2. MAIN AREA: Bao gồm Header và Vùng nội dung */}
      <div className="flex-1 flex flex-col min-w-0 h-full relative">
        {/* 3. HEADER: Cố định trên cùng của vùng Main */}
        <Header />

        {/* 4. CONTENT AREA: Vùng này sẽ cuộn (scroll) độc lập */}
        <main className="flex-1 overflow-y-auto overflow-x-hidden relative scroll-smooth custom-scrollbar">
          {/* 
                      Lưu ý: Không thêm padding ở đây vì các PageContainer 
                      (PageContainer, ListPageContainer) đã có p-6 sẵn.
                      Việc này giúp các trang có thể tùy biến padding nếu cần.
                    */}
          <Outlet />
        </main>
      </div>
    </div>
  );
};

export default Layout;
