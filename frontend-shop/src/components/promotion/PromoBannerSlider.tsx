'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import { Sparkles, Clock, ArrowRight, ShieldCheck, Zap } from 'lucide-react';
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
        }, 6000);
        return () => clearInterval(timer);
    }, [defaultPromos.length]);

    const activePromo = defaultPromos[currentIndex];

    return (
        <div className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-emerald-900 via-emerald-800 to-teal-900 text-white shadow-[0_10px_35px_-10px_rgba(5,150,105,0.25)] p-8 sm:p-12 border border-emerald-700/50">
            {/* Decorative background glow */}
            <div className="absolute top-0 right-0 -mr-24 -mt-24 w-96 h-96 rounded-full bg-emerald-400/10 blur-3xl pointer-events-none" />
            <div className="absolute bottom-0 left-0 -ml-24 -mb-24 w-96 h-96 rounded-full bg-teal-400/10 blur-3xl pointer-events-none" />

            <div className="relative z-10 flex flex-col md:flex-row items-center justify-between gap-8">
                
                {/* Left Text Block */}
                <div className="space-y-4 max-w-xl text-center md:text-left">
                    <div className="inline-flex items-center gap-2 px-3.5 py-1 bg-white/15 border border-white/20 rounded-full text-emerald-100 text-[11px] font-extrabold tracking-wide backdrop-blur-xs">
                        <Sparkles className="w-3.5 h-3.5 text-amber-300 animate-pulse" />
                        <span>CHIẾN DỊCH ƯU ĐÃI NÔNG SẢN ĐANG DIỄN RA</span>
                    </div>

                    <h2 className="text-2xl sm:text-4xl font-black tracking-tight text-white leading-tight font-sans">
                        {activePromo.name}
                    </h2>

                    <p className="text-xs sm:text-sm text-emerald-100/90 leading-relaxed font-medium">
                        Nông sản tươi thu hoạch từ các nông trại công nghệ cao Đà Lạt, bảo quản nhiệt độ chuẩn 2-8°C và giao nhanh trong 2 giờ.
                    </p>

                    <div className="flex flex-wrap items-center justify-center md:justify-start gap-4 pt-2">
                        <Link
                            href={`/khuyen-mai/${activePromo.slug}`}
                            className="inline-flex items-center gap-2 px-6 py-3.5 bg-amber-400 hover:bg-amber-300 active:bg-amber-500 text-slate-950 rounded-2xl font-extrabold text-xs shadow-md transition-all active:scale-95 cursor-pointer"
                        >
                            <span>Khám phá ngay</span>
                            <ArrowRight className="w-4 h-4" />
                        </Link>

                        <div className="flex items-center gap-1.5 text-xs text-emerald-100 font-bold bg-white/10 px-4 py-3 rounded-2xl backdrop-blur-xs border border-white/15">
                            <Zap className="w-4 h-4 text-amber-300" />
                            <span>Giảm ngay {activePromo.isPercentage ? `${activePromo.discountValue}%` : `${activePromo.discountValue}đ`}</span>
                        </div>
                    </div>
                </div>

                {/* Right Illustration / Visual Showcase */}
                <div className="relative w-48 h-48 sm:w-64 sm:h-64 flex items-center justify-center">
                    <div className="w-40 h-40 sm:w-52 sm:h-52 rounded-full bg-white/10 backdrop-blur-md border border-white/20 flex flex-col items-center justify-center text-center p-6 shadow-inner">
                        <div className="w-14 h-14 rounded-2xl bg-amber-400/20 text-amber-300 flex items-center justify-center mb-2">
                            <ShieldCheck className="w-8 h-8" />
                        </div>
                        <span className="text-[11px] uppercase tracking-widest text-emerald-200 font-bold">VietGAP Quality</span>
                        <span className="text-lg font-black text-white mt-0.5">Solaris Farm</span>
                    </div>
                </div>

            </div>

            {/* Slider Navigation Dots */}
            {defaultPromos.length > 1 && (
                <div className="flex items-center justify-center gap-2 mt-8">
                    {defaultPromos.map((_, idx) => (
                        <button
                            key={idx}
                            onClick={() => setCurrentIndex(idx)}
                            aria-label={`Slide ${idx + 1}`}
                            className={`h-2 rounded-full transition-all cursor-pointer ${
                                currentIndex === idx ? 'w-8 bg-amber-400' : 'w-2 bg-white/30 hover:bg-white/50'
                            }`}
                        />
                    ))}
                </div>
            )}
        </div>
    );
}
