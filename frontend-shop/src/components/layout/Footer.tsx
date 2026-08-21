import React from 'react';
import Link from 'next/link';
import { ShieldCheck, Truck, RotateCcw, Award, Phone, Mail, MapPin } from 'lucide-react';

export default function Footer() {
    return (
        <footer className="bg-slate-900 text-slate-300 pt-14 pb-8 border-t border-slate-800">
            {/* Features Bar */}
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pb-12 border-b border-slate-800">
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-8">
                    <div className="flex items-start gap-4">
                        <div className="p-3 bg-emerald-950/60 border border-emerald-800/60 text-emerald-400 rounded-2xl">
                            <ShieldCheck className="w-6 h-6" />
                        </div>
                        <div>
                            <h4 className="font-bold text-white text-sm">100% Nông Sản Sạch</h4>
                            <p className="text-xs text-slate-400 mt-1">Đạt chuẩn VietGAP, GlobalGAP & truy xuất vùng trồng rõ ràng.</p>
                        </div>
                    </div>

                    <div className="flex items-start gap-4">
                        <div className="p-3 bg-emerald-950/60 border border-emerald-800/60 text-emerald-400 rounded-2xl">
                            <Truck className="w-6 h-6" />
                        </div>
                        <div>
                            <h4 className="font-bold text-white text-sm">Giao Hàng Tươi Sống 2H</h4>
                            <p className="text-xs text-slate-400 mt-1">Chuỗi cung ứng lạnh bảo toàn độ tươi giòn và dinh dưỡng.</p>
                        </div>
                    </div>

                    <div className="flex items-start gap-4">
                        <div className="p-3 bg-emerald-950/60 border border-emerald-800/60 text-emerald-400 rounded-2xl">
                            <RotateCcw className="w-6 h-6" />
                        </div>
                        <div>
                            <h4 className="font-bold text-white text-sm">Đổi Trả Trong 24H</h4>
                            <p className="text-xs text-slate-400 mt-1">Đổi trả hoặc hoàn tiền 100% nếu dập nát hoặc không đạt chuẩn.</p>
                        </div>
                    </div>

                    <div className="flex items-start gap-4">
                        <div className="p-3 bg-emerald-950/60 border border-emerald-800/60 text-emerald-400 rounded-2xl">
                            <Award className="w-6 h-6" />
                        </div>
                        <div>
                            <h4 className="font-bold text-white text-sm">Giá Trực Tiếp Tại Vườn</h4>
                            <p className="text-xs text-slate-400 mt-1">Liên kết trực tiếp với các hợp tác xã nông sản sạch Đà Lạt.</p>
                        </div>
                    </div>
                </div>
            </div>

            {/* Main Links */}
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-8">
                    
                    {/* Brand Column */}
                    <div className="lg:col-span-2 space-y-4">
                        <div className="flex items-center gap-2">
                            <span className="text-2xl">🌿</span>
                            <span className="text-xl font-black tracking-tight text-white">SOLARIS ERP FARM</span>
                        </div>
                        <p className="text-xs text-slate-400 leading-relaxed max-w-sm">
                            Hệ sinh thái phân phối nông sản công nghệ cao, kết nối trực tiếp từ nông trại đến bàn ăn gia đình Việt với kỷ luật quản trị kho FEFO chuẩn quốc tế.
                        </p>
                        <div className="space-y-2 text-xs text-slate-400 pt-2">
                            <div className="flex items-center gap-2">
                                <MapPin className="w-4 h-4 text-emerald-400 shrink-0" />
                                <span>Trụ sở: Tầng 8, Tòa nhà Solaris, Quận 1, TP. Hồ Chí Minh</span>
                            </div>
                            <div className="flex items-center gap-2">
                                <Phone className="w-4 h-4 text-emerald-400 shrink-0" />
                                <span>Tổng đài CSKH: 1900 8888 (8:00 - 21:00)</span>
                            </div>
                            <div className="flex items-center gap-2">
                                <Mail className="w-4 h-4 text-emerald-400 shrink-0" />
                                <span>Email: hotro@solaris.vn</span>
                            </div>
                        </div>
                    </div>

                    {/* Về Solaris */}
                    <div>
                        <h4 className="font-bold text-white text-xs uppercase tracking-wider mb-4">Về Solaris</h4>
                        <ul className="space-y-2 text-xs text-slate-400">
                            <li><Link href="/san-pham" className="hover:text-emerald-400 transition-colors">Giới thiệu trang trại</Link></li>
                            <li><Link href="/san-pham" className="hover:text-emerald-400 transition-colors">Chứng nhận VietGAP</Link></li>
                            <li><Link href="/san-pham" className="hover:text-emerald-400 transition-colors">Quy trình kiểm soát QC</Link></li>
                            <li><Link href="/khuyen-mai" className="hover:text-emerald-400 transition-colors">Chương trình khuyến mãi</Link></li>
                        </ul>
                    </div>

                    {/* Chính sách */}
                    <div>
                        <h4 className="font-bold text-white text-xs uppercase tracking-wider mb-4">Chính Sách</h4>
                        <ul className="space-y-2 text-xs text-slate-400">
                            <li><Link href="/tai-khoan/tra-hang" className="hover:text-emerald-400 transition-colors">Chính sách đổi trả</Link></li>
                            <li><Link href="/san-pham" className="hover:text-emerald-400 transition-colors">Chính sách giao hàng 2H</Link></li>
                            <li><Link href="/san-pham" className="hover:text-emerald-400 transition-colors">Chính sách bảo mật</Link></li>
                            <li><Link href="/san-pham" className="hover:text-emerald-400 transition-colors">Nghị định 15/2018/NĐ-CP</Link></li>
                        </ul>
                    </div>

                    {/* Hỗ trợ khách hàng */}
                    <div>
                        <h4 className="font-bold text-white text-xs uppercase tracking-wider mb-4">Khách Hàng</h4>
                        <ul className="space-y-2 text-xs text-slate-400">
                            <li><Link href="/dang-nhap" className="hover:text-emerald-400 transition-colors">Đăng nhập / Đăng ký</Link></li>
                            <li><Link href="/tai-khoan/don-hang" className="hover:text-emerald-400 transition-colors">Tra cứu đơn hàng</Link></li>
                            <li><Link href="/gio-hang" className="hover:text-emerald-400 transition-colors">Giỏ hàng của bạn</Link></li>
                            <li><Link href="/tai-khoan" className="hover:text-emerald-400 transition-colors">Hạng thành viên & Ưu đãi</Link></li>
                        </ul>
                    </div>
                </div>
            </div>

            {/* Copyright */}
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pt-8 border-t border-slate-800/80 text-center text-xs text-slate-500">
                <p>© 2026 Solaris Farm E-Commerce. All rights reserved. Nền tảng quản trị ERP & Thương Mại Nông Sản.</p>
            </div>
        </footer>
    );
}
