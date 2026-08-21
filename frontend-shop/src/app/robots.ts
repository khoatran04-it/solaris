import { MetadataRoute } from 'next';

export default function robots(): MetadataRoute.Robots {
    return {
        rules: {
            userAgent: '*',
            allow: '/',
            disallow: ['/tai-khoan/', '/thanh-toan/'],
        },
        sitemap: (process.env.NEXT_PUBLIC_SITE_URL || 'https://solaris.vn') + '/sitemap.xml',
    };
}

