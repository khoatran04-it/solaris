import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { 
    AttributeDefinition, 
    AttributeDefinitionPayload, 
    AttributeDefinitionQueryParams 
} from '../types/attributeDefinition';

export const attributeDefinitionApi = {
    getAll: (params?: AttributeDefinitionQueryParams): Promise<PagedResult<AttributeDefinition>> => {
        return axiosClient.get('/AttributeDefinitions', { params });
    },

    getAllList: (): Promise<AttributeDefinition[]> => {
        return axiosClient.get('/AttributeDefinitions/all');
    },

    getById: (id: number): Promise<AttributeDefinition> => {
        return axiosClient.get(`/AttributeDefinitions/${id}`);
    },

    create: (data: AttributeDefinitionPayload): Promise<{ message: string; id: number }> => {
        return axiosClient.post('/AttributeDefinitions', data);
    },

    update: (id: number, data: AttributeDefinitionPayload): Promise<void> => {
        return axiosClient.put(`/AttributeDefinitions/${id}`, data);
    },

    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/AttributeDefinitions/${id}`);
    },
};