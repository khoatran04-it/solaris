'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import { ChevronLeft, ChevronRight, Sparkles, Clock, ArrowRight } from 'lucide-react';
import { ShopPromotionBadge } from '@/types/product';

interface PromoBannerSliderProps {
    promotions: ShopPromotionBadge[];
}

export default function PromoBannerSlider({ promotions }: PromoBannerSliderProps) {
    const [currentIndex, setCurrentIndex] = useState(0);

    const defaultPromos = promotions.length > 0 ? promotions : [
        {
            id: 1,
            name: 'Đại Tiệc Nông Sản Sạch Đà Lạt - Giảm Đến 30%',
            slug: 'dai-tiec-nong-san-sach',
            isPercentage: true,
            discountValue: 30,
            endDate: new Date(Date.now() + 7 * 86400000).toISOString()
        }
    ];

    useEffect(() => {
        if (defaultPromos.length <= 1) return;
        const timer = setInterval(() => {
            setCurrentIndex((prev) => (prev + 1) % defaultPromos.length);
        }, 5000);
        return () => clearInterval(timer);
    }, [defaultPromos.length]);

    const activePromo = defaultPromos[currentIndex];

    return (
        <div className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-emerald-800 via-teal-800 to-slate-950 text-white shadow-2xl p-6 sm:p-10 border border-emerald-500/20">
            {/* Decorative background glow */}
            <div className="absolute top-0 right-0 -mr-20 -mt-20 w-80 h-80 rounded-full bg-emerald-400/10 blur-3xl pointer-events-none" />
            <div className="absolute bottom-0 left-0 -ml-20 -mb-20 w-80 h-80 rounded-full bg-teal-400/10 blur-3xl pointer-events-none" />

            <div className="relative z-10 flex flex-col md:flex-row items-center justify-between gap-8">
                
                {/* Left Text Block */}
                <div className="space-y-4 max-w-xl text-center md:text-left">
                    <div className="inline-flex items-center gap-2 px-3 py-1 bg-emerald-500/20 border border-emerald-400/30 rounded-full text-emerald-300 text-xs font-bold tracking-wide">
                        <Sparkles className="w-3.5 h-3.5 text-amber-300 animate-spin" />
                        <span>CHIẾN DỊCH KHUYẾN MÃI ĐANG DIỄN RA</span>
                    </div>

                    <h2 className="text-2xl sm:text-4xl font-black tracking-tight text-white leading-tight">
                        {activePromo.name}
                    </h2>

                    <p className="text-xs sm:text-sm text-emerald-100/80 leading-relaxed">
                        Nông sản tuyển chọn trực tiếp từ các nông trại công nghệ cao, thu hoạch mỗi sáng và giao trong 2 giờ.
                    </p>

                    <div className="flex flex-wrap items-center justify-center md:justify-start gap-4 pt-2">
                        <Link
                            href={`/khuyen-mai/${activePromo.slug}`}
                            className="inline-flex items-center gap-2 px-6 py-3 bg-gradient-to-r from-emerald-500 to-teal-500 hover:from-emerald-400 hover:to-teal-400 text-white rounded-full font-bold text-xs shadow-lg shadow-emerald-500/30 hover:scale-105 active:scale-95 transition-all"
                        >
                            <span>Khám phá ngay</span>
                            <ArrowRight className="w-4 h-4" />
                        </Link>

                        <div className="flex items-center gap-1.5 text-xs text-amber-300 font-semibold bg-white/10 px-4 py-2.5 rounded-full backdrop-blur-xs">
                            <Clock className="w-4 h-4" />
                            <span>Giảm ngay {activePromo.isPercentage ? `${activePromo.discountValue}%` : `${activePromo.discountValue}đ`}</span>
                        </div>
                    </div>
                </div>

                {/* Right Illustration / Graphic */}
                <div className="relative w-48 h-48 sm:w-64 sm:h-64 flex items-center justify-center">
                    <div className="absolute inset-0 bg-gradient-to-tr from-emerald-500/20 to-teal-400/30 rounded-full animate-pulse blur-xl" />
                    <div className="relative text-7xl sm:text-8xl select-none filter drop-shadow-2xl">
                        🥑🍓🍎
                    </div>
                </div>

            </div>

            {/* Slider Navigation Dots */}
            {defaultPromos.length > 1 && (
                <div className="flex items-center justify-center gap-2 mt-6">
                    {defaultPromos.map((_, idx) => (
                        <button
                            key={idx}
                            onClick={() => setCurrentIndex(idx)}
                            className={`h-2 rounded-full transition-all ${
                                currentIndex === idx ? 'w-8 bg-emerald-400' : 'w-2 bg-white/30 hover:bg-white/60'
                            }`}
                        />
                    ))}
                </div>
            )}
        </div>
    );
}
