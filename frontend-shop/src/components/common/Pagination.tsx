'use client';

import React from 'react';
import Link from 'next/link';
import { ChevronLeft, ChevronRight } from 'lucide-react';

interface PaginationProps {
    currentPage: number;
    totalPages: number;
    baseUrl: string;
    searchParams?: Record<string, string | undefined>;
    className?: string;
}

export default function Pagination({
    currentPage,
    totalPages,
    baseUrl,
    searchParams = {},
    className = ''
}: PaginationProps) {
    if (totalPages <= 1) return null;

    const buildPageUrl = (page: number) => {
        const params = new URLSearchParams();
        Object.entries(searchParams).forEach(([key, val]) => {
            if (val && key !== 'page') {
                params.set(key, val);
            }
        });
        params.set('page', page.toString());
        const queryStr = params.toString();
        return queryStr ? `${baseUrl}?${queryStr}` : baseUrl;
    };

    // Tạo danh sách trang hiển thị gọn gàng (có ... nếu nhiều trang)
    const getVisiblePages = () => {
        const pages: (number | string)[] = [];
        if (totalPages <= 7) {
            for (let i = 1; i <= totalPages; i++) pages.push(i);
        } else {
            if (currentPage <= 4) {
                pages.push(1, 2, 3, 4, 5, '...', totalPages);
            } else if (currentPage >= totalPages - 3) {
                pages.push(1, '...', totalPages - 4, totalPages - 3, totalPages - 2, totalPages - 1, totalPages);
            } else {
                pages.push(1, '...', currentPage - 1, currentPage, currentPage + 1, '...', totalPages);
            }
        }
        return pages;
    };

    return (
        <div className={`flex items-center justify-center gap-1.5 pt-8 border-t border-slate-200/80 ${className}`}>
            {/* Previous Page */}
            {currentPage > 1 ? (
                <Link
                    href={buildPageUrl(currentPage - 1)}
                    className="w-10 h-10 rounded-2xl bg-white text-slate-700 hover:bg-slate-100 border border-slate-200/80 flex items-center justify-center transition-all shadow-2xs hover:border-slate-300"
                    title="Trang trước"
                >
                    <ChevronLeft className="w-4 h-4" />
                </Link>
            ) : (
                <span className="w-10 h-10 rounded-2xl bg-slate-50 text-slate-300 border border-slate-100 flex items-center justify-center cursor-not-allowed">
                    <ChevronLeft className="w-4 h-4" />
                </span>
            )}

            {/* Page Numbers */}
            {getVisiblePages().map((p, idx) => {
                if (typeof p === 'string') {
                    return (
                        <span key={`ellipsis-${idx}`} className="w-8 text-center text-xs font-bold text-slate-400">
                            ...
                        </span>
                    );
                }

                const isCurrent = p === currentPage;

                return (
                    <Link
                        key={p}
                        href={buildPageUrl(p)}
                        className={`w-10 h-10 rounded-2xl flex items-center justify-center text-xs font-bold transition-all ${
                            isCurrent
                                ? 'bg-emerald-700 text-white shadow-sm shadow-emerald-700/25 border border-emerald-700'
                                : 'bg-white text-slate-700 hover:bg-slate-100 border border-slate-200/80 shadow-2xs hover:border-slate-300'
                        }`}
                    >
                        {p}
                    </Link>
                );
            })}

            {/* Next Page */}
            {currentPage < totalPages ? (
                <Link
                    href={buildPageUrl(currentPage + 1)}
                    className="w-10 h-10 rounded-2xl bg-white text-slate-700 hover:bg-slate-100 border border-slate-200/80 flex items-center justify-center transition-all shadow-2xs hover:border-slate-300"
                    title="Trang tiếp"
                >
                    <ChevronRight className="w-4 h-4" />
                </Link>
            ) : (
                <span className="w-10 h-10 rounded-2xl bg-slate-50 text-slate-300 border border-slate-100 flex items-center justify-center cursor-not-allowed">
                    <ChevronRight className="w-4 h-4" />
                </span>
            )}
        </div>
    );
}
