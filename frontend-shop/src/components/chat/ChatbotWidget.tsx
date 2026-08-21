'use client';

import React, { useState } from 'react';
import { Bot, X, Sparkles, Send, MessageSquare, ArrowRight, ShieldAlert } from 'lucide-react';

export default function ChatbotWidget() {
    const [isOpen, setIsOpen] = useState(false);
    const [inputMessage, setInputMessage] = useState('');

    const sampleQueries = [
        '🍎 Tìm táo có độ ngọt Brix trên 14°',
        '📦 Kiểm tra tiến độ đơn hàng ORD-xxx',
        '🌿 Trái cây nào đạt chuẩn VietGAP?',
        '🥑 Cách bảo quản bơ 034 không bị thâm'
    ];

    return (
        <div className="fixed bottom-6 right-6 z-50">
            {/* Floating Toggle Button */}
            {!isOpen && (
                <button
                    onClick={() => setIsOpen(true)}
                    className="group relative flex items-center gap-2.5 px-4 py-3.5 bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white rounded-full shadow-2xl shadow-emerald-600/40 hover:scale-105 active:scale-95 transition-all duration-200"
                >
                    <span className="relative flex h-3 w-3">
                        <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-300 opacity-75"></span>
                        <span className="relative inline-flex rounded-full h-3 w-3 bg-white"></span>
                    </span>

                    <Bot className="w-5 h-5" />
                    <span className="text-xs font-bold tracking-wide">Trợ Lý Nông Sản AI</span>
                    
                    <span className="px-1.5 py-0.5 bg-emerald-950/40 text-[10px] font-extrabold rounded-md text-emerald-200 border border-emerald-400/30">
                        AI
                    </span>
                </button>
            )}

            {/* Chat Modal / Drawer */}
            {isOpen && (
                <div className="w-[360px] sm:w-[400px] h-[520px] bg-white rounded-3xl shadow-2xl border border-emerald-100 flex flex-col overflow-hidden animate-in fade-in slide-in-from-bottom-4 duration-200">
                    
                    {/* Header */}
                    <div className="bg-gradient-to-r from-emerald-600 to-teal-700 p-4 text-white flex items-center justify-between">
                        <div className="flex items-center gap-3">
                            <div className="w-10 h-10 rounded-2xl bg-white/10 backdrop-blur-sm border border-white/20 flex items-center justify-center text-xl shadow-inner">
                                🤖
                            </div>
                            <div>
                                <h3 className="font-bold text-sm leading-tight flex items-center gap-1.5">
                                    Solaris AI Advisor
                                    <span className="px-1.5 py-0.2 bg-emerald-400/30 text-[9px] font-bold rounded text-emerald-100 uppercase">
                                        Phase 1 Preview
                                    </span>
                                </h3>
                                <p className="text-[11px] text-emerald-100/90 flex items-center gap-1 mt-0.5">
                                    <span className="w-1.5 h-1.5 rounded-full bg-emerald-300 animate-pulse"></span>
                                    Sẵn sàng kết nối Gemini 2.0 Flash
                                </p>
                            </div>
                        </div>

                        <button
                            onClick={() => setIsOpen(false)}
                            className="p-1.5 text-white/80 hover:text-white hover:bg-white/10 rounded-full transition-colors"
                        >
                            <X className="w-5 h-5" />
                        </button>
                    </div>

                    {/* Chat Body */}
                    <div className="flex-1 p-4 overflow-y-auto space-y-4 bg-slate-50">
                        
                        {/* Bot Introduction Message */}
                        <div className="flex items-start gap-2.5">
                            <div className="w-7 h-7 rounded-full bg-emerald-600 text-white flex items-center justify-center text-xs shrink-0 font-bold">
                                AI
                            </div>
                            <div className="bg-white p-3.5 rounded-2xl rounded-tl-xs shadow-xs border border-slate-100 text-xs text-slate-700 space-y-2 leading-relaxed">
                                <p className="font-bold text-slate-900 flex items-center gap-1">
                                    <Sparkles className="w-3.5 h-3.5 text-amber-500" />
                                    Xin chào Quý khách!
                                </p>
                                <p>
                                    Tôi là <strong>Trợ Lý Ảo Nông Sản Solaris</strong>. Hiện tại hệ thống đang ở <strong>Phase 1 (Nền tảng Shop MVP)</strong>.
                                </p>
                                <p className="text-slate-600">
                                    Ở <strong>Phase 3</strong>, tôi sẽ được trang bị mô hình <strong>Gemini 2.0 Flash + RAG Knowledge Base</strong> để giúp bạn:
                                </p>
                                <ul className="list-disc list-inside space-y-1 text-slate-600 pl-1 text-[11px]">
                                    <li>Tìm kiếm trái cây theo độ ngọt Brix & vùng trồng</li>
                                    <li>Tư vấn nông sản sạch chuẩn VietGAP/GlobalGAP</li>
                                    <li>Tra cứu hành trình đơn hàng theo thời gian thực</li>
                                    <li>Thêm sản phẩm trực tiếp vào giỏ bằng câu lệnh tự nhiên</li>
                                </ul>
                            </div>
                        </div>

                        {/* Sample Suggestion Queries */}
                        <div className="space-y-1.5 pt-2">
                            <p className="text-[10px] uppercase font-bold tracking-wider text-slate-400 px-1">
                                Câu hỏi mẫu (Sẽ kích hoạt ở Phase 3):
                            </p>
                            {sampleQueries.map((query, index) => (
                                <button
                                    key={index}
                                    onClick={() => setInputMessage(query)}
                                    className="w-full text-left text-xs p-2.5 bg-white hover:bg-emerald-50 text-slate-700 hover:text-emerald-700 rounded-xl border border-slate-200/80 hover:border-emerald-300 transition-all flex items-center justify-between group"
                                >
                                    <span>{query}</span>
                                    <ArrowRight className="w-3.5 h-3.5 text-slate-300 group-hover:text-emerald-600 transition-colors" />
                                </button>
                            ))}
                        </div>
                    </div>

                    {/* Input Bar */}
                    <div className="p-3 bg-white border-t border-slate-100">
                        <form
                            onSubmit={(e) => {
                                e.preventDefault();
                                if (!inputMessage.trim()) return;
                                alert(`Cảm ơn bạn! Tính năng Chat AI thông minh sẽ được kích hoạt toàn diện trong Phase 3.`);
                                setInputMessage('');
                            }}
                            className="flex items-center gap-2"
                        >
                            <input
                                type="text"
                                placeholder="Nhập câu hỏi (Phase 3 AI)..."
                                value={inputMessage}
                                onChange={(e) => setInputMessage(e.target.value)}
                                className="flex-1 px-3.5 py-2 bg-slate-50 border border-slate-200 rounded-full text-xs focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:bg-white"
                            />
                            <button
                                type="submit"
                                className="p-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-full transition-colors shadow-xs"
                            >
                                <Send className="w-4 h-4" />
                            </button>
                        </form>
                    </div>

                </div>
            )}
        </div>
    );
}
