"use client";

import React from "react";
import Link from "next/link";
import { Package } from "lucide-react";

interface EmptyStateProps {
  icon?: React.ReactNode;
  title: string;
  description?: string;
  actionText?: string;
  actionHref?: string;
  onAction?: () => void;
  className?: string;
}

export default function EmptyState({
  icon,
  title,
  description,
  actionText,
  actionHref,
  onAction,
  className = "",
}: EmptyStateProps) {
  return (
    <div
      className={`bg-white rounded-3xl border border-slate-200/80 p-12 sm:p-16 text-center space-y-4 shadow-2xs ${className}`}
    >
      <div className="w-14 h-14 rounded-2xl bg-emerald-50/80 text-emerald-700 flex items-center justify-center mx-auto border border-emerald-100/60 shadow-2xs">
        {icon || <Package className="w-7 h-7" />}
      </div>

      <div className="space-y-1 max-w-md mx-auto">
        <h3 className="text-base sm:text-lg font-bold text-slate-900 tracking-tight">
          {title}
        </h3>
        {description && (
          <p className="text-xs text-slate-500 leading-relaxed font-normal">
            {description}
          </p>
        )}
      </div>

      {actionText && (
        <div className="pt-2">
          {actionHref ? (
            <Link
              href={actionHref}
              className="inline-flex items-center justify-center px-5 py-2.5 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white text-xs font-bold rounded-2xl transition-all shadow-sm hover:shadow-md hover:shadow-emerald-600/20 active:scale-95"
            >
              {actionText}
            </Link>
          ) : onAction ? (
            <button
              type="button"
              onClick={onAction}
              className="inline-flex items-center justify-center px-5 py-2.5 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white text-xs font-bold rounded-2xl transition-all shadow-sm hover:shadow-md hover:shadow-emerald-600/20 active:scale-95 cursor-pointer"
            >
              {actionText}
            </button>
          ) : null}
        </div>
      )}
    </div>
  );
}
