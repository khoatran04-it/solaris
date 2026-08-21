import { MetadataRoute } from 'next';
import { apiClient } from '@/lib/api';
import { PagedResult, ShopProductCard, ShopCategoryTree, ShopPromotionBadge } from '@/types/shop';

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
    const baseUrl = 'https://solaris.vn';
    const now = new Date();

    const staticRoutes: MetadataRoute.Sitemap = [
        { url: baseUrl, lastModified: now, changeFrequency: 'daily', priority: 1.0 },
        { url: `${baseUrl}/san-pham`, lastModified: now, changeFrequency: 'daily', priority: 0.9 },
        { url: `${baseUrl}/khuyen-mai`, lastModified: now, changeFrequency: 'daily', priority: 0.8 },
        { url: `${baseUrl}/dang-nhap`, lastModified: now, changeFrequency: 'monthly', priority: 0.3 },
        { url: `${baseUrl}/dang-ky`, lastModified: now, changeFrequency: 'monthly', priority: 0.3 },
    ];

    try {
        const [prodRes, categories, promos] = await Promise.all([
            apiClient.get<PagedResult<ShopProductCard>>('/products?pageSize=100').catch(() => ({ items: [] })),
            apiClient.get<ShopCategoryTree[]>('/products/categories').catch(() => []),
            apiClient.get<ShopPromotionBadge[]>('/products/promotions').catch(() => []),
        ]);

        const productRoutes: MetadataRoute.Sitemap = (prodRes.items || []).map((p) => ({
            url: `${baseUrl}/san-pham/${p.slug}`,
            lastModified: now,
            changeFrequency: 'weekly',
            priority: 0.8,
        }));

        const categoryRoutes: MetadataRoute.Sitemap = categories.flatMap((g) => [
            { url: `${baseUrl}/danh-muc/${g.groupSlug}`, lastModified: now, changeFrequency: 'weekly', priority: 0.7 },
            ...g.categories.map((c) => ({
                url: `${baseUrl}/danh-muc/${c.categorySlug}`,
                lastModified: now,
                changeFrequency: 'weekly' as const,
                priority: 0.7,
            }))
        ]);

        const promoRoutes: MetadataRoute.Sitemap = promos.map((pr) => ({
            url: `${baseUrl}/khuyen-mai/${pr.slug}`,
            lastModified: now,
            changeFrequency: 'weekly',
            priority: 0.6,
        }));

        return [...staticRoutes, ...categoryRoutes, ...productRoutes, ...promoRoutes];
    } catch {
        return staticRoutes;
    }
}
