import type { Metadata } from 'next';
import './globals.css';
import Header from '@/components/layout/Header';
import Footer from '@/components/layout/Footer';
import ChatbotWidget from '@/components/chat/ChatbotWidget';

export const metadata: Metadata = {
    title: {
        template: '%s | Solaris Nông Sản Sạch',
        default: 'Solaris Farm - Hệ Thống Nông Sản Sạch Chuẩn VietGAP / GlobalGAP'
    },
    description: 'Solaris Farm cung cấp rau củ quả, trái cây tươi sạch trực tiếp từ nông trại Đà Lạt đến bàn ăn. Giao nhanh 2h, bảo toàn độ tươi giòn, truy xuất nguồn gốc minh bạch.',
    keywords: ['nông sản sạch', 'trái cây nhập khẩu', 'rau củ đà lạt', 'vietgap', 'globalgap', 'thực phẩm sạch', 'solaris farm'],
    authors: [{ name: 'Solaris ERP Farm' }],
    openGraph: {
        type: 'website',
        locale: 'vi_VN',
        url: 'https://solaris.vn',
        siteName: 'Solaris Nông Sản Sạch',
        title: 'Solaris Farm - Nông Sản Sạch Chuẩn VietGAP',
        description: 'Phân phối nông sản tươi ngon, an toàn từ nông trại công nghệ cao.',
    },
    robots: {
        index: true,
        follow: true
    }
};

export default function RootLayout({
    children,
}: {
    children: React.ReactNode;
}) {
    return (
        <html lang="vi" className="scroll-smooth">
            <body className="min-h-screen flex flex-col bg-white text-slate-800 antialiased font-sans">
                <Header />
                <main className="flex-1">
                    {children}
                </main>
                <Footer />
                <ChatbotWidget />
            </body>
        </html>
    );
}
