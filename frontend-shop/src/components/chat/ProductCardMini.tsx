import React from 'react';
import Link from 'next/link';
import { MapPin, Award, ArrowRight } from 'lucide-react';
import { AiProductCard } from '@/types/chat';
import { formatVND } from '@/lib/utils';

interface ProductCardMiniProps {
    product: AiProductCard;
}

export default function ProductCardMini({ product }: ProductCardMiniProps) {
    return (
        <div className="bg-white rounded-2xl border border-slate-200 p-3 shadow-xs hover:shadow-md hover:border-emerald-300 transition-all flex gap-3 items-center">
            {/* Image */}
            <Link href={`/san-pham/${product.slug}`} className="w-16 h-16 rounded-xl bg-slate-50 overflow-hidden shrink-0 flex items-center justify-center border border-slate-100">
                {product.imagePath ? (
                    <img src={product.imagePath} alt={product.name} className="w-full h-full object-cover" />
                ) : (
                    <span className="text-2xl">🥑</span>
                )}
            </Link>

            {/* Info */}
            <div className="flex-1 min-w-0 space-y-1">
                <div className="flex items-center gap-1.5 text-[9px]">
                    {product.origin && (
                        <span className="inline-flex items-center gap-0.5 px-1.5 py-0.5 bg-slate-100 text-slate-600 rounded font-medium">
                            <MapPin className="w-2.5 h-2.5 text-emerald-600" />
                            {product.origin}
                        </span>
                    )}
                    {product.certification && (
                        <span className="inline-flex items-center gap-0.5 px-1.5 py-0.5 bg-emerald-50 text-emerald-700 rounded font-bold">
                            <Award className="w-2.5 h-2.5 text-emerald-600" />
                            {product.certification}
                        </span>
                    )}
                </div>

                <h4 className="font-bold text-xs text-slate-900 truncate">
                    <Link href={`/san-pham/${product.slug}`} className="hover:text-emerald-700">
                        {product.name}
                    </Link>
                </h4>

                <div className="flex items-baseline gap-1">
                    <span className="font-black text-emerald-700 text-xs">
                        {formatVND(product.discountedPrice || product.price)}
                    </span>
                    <span className="text-[10px] text-slate-400">/{product.uoMName || 'Kg'}</span>
                </div>
            </div>

            {/* Link Action */}
            <Link
                href={`/san-pham/${product.slug}`}
                className="w-7 h-7 rounded-full bg-emerald-50 text-emerald-700 hover:bg-emerald-600 hover:text-white transition-colors flex items-center justify-center shrink-0"
                title="Xem chi tiết"
            >
                <ArrowRight className="w-3.5 h-3.5" />
            </Link>
        </div>
    );
}
