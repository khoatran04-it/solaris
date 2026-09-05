'use client';

import React from 'react';
import Link from 'next/link';
import { User, Package, MapPin, RotateCcw } from 'lucide-react';

interface AccountSidebarProps {
    activeTab: 'profile' | 'orders' | 'addresses' | 'returns';
    className?: string;
}

export default function AccountSidebar({ activeTab, className = '' }: AccountSidebarProps) {
    const navItems = [
        {
            key: 'profile',
            href: '/tai-khoan',
            label: 'Hồ sơ cá nhân & Thẻ VIP',
            icon: User,
        },
        {
            key: 'orders',
            href: '/tai-khoan/don-hang',
            label: 'Lịch sử đơn hàng',
            icon: Package,
        },
        {
            key: 'addresses',
            href: '/tai-khoan/dia-chi',
            label: 'Sổ địa chỉ nhận hàng',
            icon: MapPin,
        },
        {
            key: 'returns',
            href: '/tai-khoan/tra-hang',
            label: 'Yêu cầu đổi trả nông sản (RMA)',
            icon: RotateCcw,
        },
    ];

    return (
        <aside className={`bg-white rounded-3xl border border-slate-200/80 p-5 space-y-1.5 shadow-[0_2px_15px_-3px_rgba(0,0,0,0.03)] ${className}`}>
            {navItems.map((item) => {
                const Icon = item.icon;
                const isActive = activeTab === item.key;

                return (
                    <Link
                        key={item.key}
                        href={item.href}
                        className={`flex items-center gap-3 px-4 py-3 rounded-2xl text-xs transition-colors ${
                            isActive
                                ? 'bg-emerald-600 text-white font-extrabold shadow-xs'
                                : 'text-slate-700 hover:bg-emerald-50 hover:text-emerald-800 font-bold'
                        }`}
                    >
                        <Icon className={`w-4 h-4 ${isActive ? 'text-white' : 'text-slate-400'}`} />
                        <span>{item.label}</span>
                    </Link>
                );
            })}
        </aside>
    );
}
