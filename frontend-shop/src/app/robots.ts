import { MetadataRoute } from 'next';

export default function robots(): MetadataRoute.Robots {
    return {
        rules: {
            userAgent: '*',
            allow: '/',
            disallow: ['/tai-khoan/', '/thanh-toan/'],
        },
        sitemap: 'https://solaris.vn/sitemap.xml',
    };
}
