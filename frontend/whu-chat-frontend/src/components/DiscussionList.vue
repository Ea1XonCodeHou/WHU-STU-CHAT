<template>
  <div class="discussion-list">
    <div class="header">
      <h3>讨论区</h3>
      <div class="header-actions">
      </div>
    </div>

    <div class="search-box">
      <i class="fas fa-search"></i>
      <input 
        type="text" 
        v-model="searchQuery" 
        placeholder="搜索讨论区..." 
        @input="filterDiscussions"
      />
      <i 
        v-if="searchQuery" 
        class="fas fa-times-circle" 
        @click="clearSearch"
      ></i>
    </div>

    <div class="hot-discussions" v-if="hotDiscussions.length > 0">
      <div class="section-title">
        <i class="fas fa-fire"></i>
        <span>热门讨论区</span>
      </div>
      <div class="discussion-items">
        <div 
          v-for="discussion in hotDiscussions" 
          :key="discussion.discussionId"
          class="discussion-item" 
          :class="{ active: selectedDiscussionId === discussion.discussionId }"
          @click="selectDiscussion(discussion)"
        >
          <div class="discussion-icon">
            <i class="fas fa-comments"></i>
          </div>
          <div class="discussion-info">
            <h3>{{ discussion.title }}</h3>
            <p>{{ discussion.description }}</p>
          </div>
          <div class="hot-badge" v-if="discussion.isHot">
            <i class="fas fa-fire"></i>
          </div>
        </div>
      </div>
    </div>

    <div class="all-discussions">
      <div class="section-title" v-if="hotDiscussions.length > 0">
        <i class="fas fa-list"></i>
        <span>所有讨论区</span>
      </div>
      <div class="discussion-items">
        <div 
          v-for="discussion in filteredDiscussions" 
          :key="discussion.discussionId"
          class="discussion-item" 
          :class="{ active: selectedDiscussionId === discussion.discussionId }"
          @click="selectDiscussion(discussion)"
        >
          <div class="discussion-icon">
            <i class="fas fa-comments"></i>
          </div>
          <div class="discussion-info">
            <h3>{{ discussion.title }}</h3>
            <p>{{ discussion.description }}</p>
          </div>
          <div class="hot-badge" v-if="discussion.isHot">
            <i class="fas fa-fire"></i>
          </div>
        </div>

        <div v-if="filteredDiscussions.length === 0" class="empty-state">
          <i class="fas fa-search"></i>
          <p v-if="searchQuery">未找到匹配"{{ searchQuery }}"的讨论区</p>
          <p v-else>暂无讨论区</p>
        </div>
      </div>
    </div>

    <!-- 创建讨论区模态窗口 -->
    <div v-if="showCreateModal" class="modal-overlay" @click="showCreateModal = false">
      <div class="modal-container" @click.stop>
        <div class="modal-header">
          <h3>创建新讨论区</h3>
          <button class="close-button" @click="showCreateModal = false">
            <i class="fas fa-times"></i>
          </button>
        </div>
        <div class="modal-body">
          <form @submit.prevent="createDiscussion">
            <div class="form-group">
              <label for="title">标题</label>
              <input 
                id="title" 
                type="text" 
                v-model="newDiscussion.title" 
                placeholder="讨论区标题"
                required
              />
            </div>
            <div class="form-group">
              <label for="description">描述</label>
              <textarea 
                id="description" 
                v-model="newDiscussion.description" 
                placeholder="讨论区描述"
                required
              ></textarea>
            </div>
            <div class="form-group checkbox">
              <input 
                id="isHot" 
                type="checkbox" 
                v-model="newDiscussion.isHot"
              />
              <label for="isHot">设为热门</label>
            </div>
            <div class="form-actions">
              <button type="button" class="cancel-button" @click="showCreateModal = false">取消</button>
              <button type="submit" class="submit-button" :disabled="isSubmitting">
                <span v-if="isSubmitting">
                  <i class="fas fa-spinner fa-spin"></i> 创建中...
                </span>
                <span v-else>创建讨论区</span>
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, computed, onMounted, watch } from 'vue';
import axios from 'axios';

export default {
  name: 'DiscussionList',
  props: {
    discussions: {
      type: Array,
      default: () => []
    },
    selectedDiscussionId: {
      type: Number,
      default: null
    }
  },
  emits: ['select-discussion', 'refresh-discussions'],
  setup(props, { emit }) {
    const searchQuery = ref('');
    const filteredDiscussions = ref([]);
    const showCreateModal = ref(false);
    const isSubmitting = ref(false);
    const selectedDiscussionId = ref(props.selectedDiscussionId);
    
    // 新讨论区表单数据
    const newDiscussion = ref({
      title: '',
      description: '',
      isHot: false
    });
    
    // 用于存储本地筛选结果
    const filterDiscussions = () => {
      if (!searchQuery.value) {
        filteredDiscussions.value = props.discussions;
        return;
      }
      
      const query = searchQuery.value.toLowerCase();
      filteredDiscussions.value = props.discussions.filter(discussion => 
        discussion.title.toLowerCase().includes(query) || 
        discussion.description.toLowerCase().includes(query)
      );
    };
    
    // 清除搜索
    const clearSearch = () => {
      searchQuery.value = '';
      filterDiscussions();
    };
    
    // 热门讨论区列表
    const hotDiscussions = computed(() => {
      return props.discussions.filter(d => d.isHot);
    });

    // 选择讨论区
    const selectDiscussion = (discussion) => {
      try {
        if (!discussion || !discussion.discussionId) {
          console.error('讨论区数据无效');
          return;
        }
        selectedDiscussionId.value = discussion.discussionId;
        emit('select-discussion', {...discussion});
      } catch (err) {
        console.error('选择讨论区错误:', err);
      }
    };
    
    // 创建新讨论区
    const createDiscussion = async () => {
      if (!newDiscussion.value.title || !newDiscussion.value.description) {
        alert('请填写完整的讨论区信息');
        return;
      }
      
      isSubmitting.value = true;
      try {
        const userId = localStorage.getItem('userId');
        if (!userId) {
          alert('请先登录');
          return;
        }
        
        const discussionData = {
          title: newDiscussion.value.title,
          description: newDiscussion.value.description,
          creatorId: parseInt(userId),
          isHot: newDiscussion.value.isHot
        };
        
        const response = await axios.post('/api/discussion', discussionData);
        console.log('创建讨论区成功:', response.data);
        
        // 重置表单并关闭模态窗口
        newDiscussion.value = {
          title: '',
          description: '',
          isHot: false
        };
        showCreateModal.value = false;
        
        // 刷新讨论区列表
        refreshDiscussions();
      } catch (error) {
        console.error('创建讨论区失败:', error);
        alert('创建讨论区失败: ' + (error.response?.data || error.message));
      } finally {
        isSubmitting.value = false;
      }
    };
    
    // 刷新讨论区列表
    const refreshDiscussions = async () => {
      try {
        const response = await axios.get('/api/discussion');
        emit('refresh-discussions', response.data);
      } catch (error) {
        console.error('刷新讨论区列表失败:', error);
      }
    };
    
    // 监听讨论区列表变化
    watch(() => props.discussions, (newVal) => {
      if (newVal) {
        filterDiscussions();
      }
    }, { immediate: true, deep: true });
    
    // 初始化
    onMounted(() => {
      // 初始时从所有讨论区中筛选
      filterDiscussions();
    });

    return {
      searchQuery,
      showCreateModal,
      isSubmitting,
      newDiscussion,
      filteredDiscussions,
      hotDiscussions,
      filterDiscussions,
      clearSearch,
      selectDiscussion,
      createDiscussion,
      refreshDiscussions,
      selectedDiscussionId: ref(props.selectedDiscussionId)
    };
  }
};
</script>

<style scoped>
.discussion-list {
  display: flex;
  flex-direction: column;
  height: 100%;
  background-color: white;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
  border-bottom: 1px solid #eaeaea;
}

.header h3 {
  font-size: 22px;
  color: #333;
  margin: 0;
}

.header-actions {
  display: flex;
  gap: 10px;
}

.refresh-button {
  background: none;
  border: none;
  width: 36px;
  height: 36px;
  border-radius: 18px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s ease;
}

.refresh-button:hover {
  transform: scale(1.1);
}

.search-box {
  display: flex;
  align-items: center;
  padding: 10px 20px;
  border-bottom: 1px solid #eaeaea;
  background-color: #f8f9fa;
  position: relative;
}

.search-box i {
  color: #999;
  margin-right: 8px;
}

.search-box input {
  flex: 1;
  border: none;
  background: transparent;
  outline: none;
  font-size: 14px;
  padding: 8px 0;
}

.search-box .fa-times-circle {
  cursor: pointer;
  color: #999;
  margin-left: 8px;
  margin-right: 0;
}

.section-title {
  display: flex;
  align-items: center;
  padding: 10px 20px;
  font-size: 14px;
  color: #666;
  text-transform: uppercase;
  letter-spacing: 1px;
  font-weight: 600;
}

.section-title i {
  margin-right: 8px;
  color: #4776E6;
}

.hot-discussions {
  border-bottom: 1px solid #eaeaea;
}

.all-discussions,
.hot-discussions {
  overflow: auto;
}

.hot-discussions .discussion-items {
  max-height: 200px;
  overflow-y: auto;
}

.all-discussions .discussion-items {
  flex: 1;
  overflow-y: auto;
}

.discussion-items {
  padding: 0 10px;
}

.discussion-item {
  display: flex;
  align-items: center;
  padding: 15px;
  border-radius: 8px;
  margin-bottom: 10px;
  cursor: pointer;
  transition: all 0.2s ease;
  position: relative;
}

.discussion-item:hover {
  background-color: #f5f7fb;
}

.discussion-item.active {
  background-color: #f0f5ff;
  box-shadow: 0 2px 10px rgba(71, 118, 230, 0.1);
}

.discussion-icon {
  min-width: 40px;
  height: 40px;
  border-radius: 20px;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  margin-right: 12px;
}

.discussion-info {
  flex: 1;
  overflow: hidden;
}

.discussion-info h3 {
  margin: 0 0 5px;
  font-size: 16px;
  color: #333;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.discussion-info p {
  margin: 0;
  font-size: 12px;
  color: #666;
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}

.hot-badge {
  position: absolute;
  top: 10px;
  right: 10px;
  width: 20px;
  height: 20px;
  border-radius: 10px;
  background-color: #ff5252;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 10px;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 40px 20px;
  color: #999;
  text-align: center;
}

.empty-state i {
  font-size: 40px;
  margin-bottom: 10px;
}

/* 模态窗口样式 */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-container {
  width: 90%;
  max-width: 500px;
  background-color: white;
  border-radius: 8px;
  box-shadow: 0 5px 20px rgba(0, 0, 0, 0.2);
  overflow: hidden;
  animation: modalFadeIn 0.3s ease;
}

@keyframes modalFadeIn {
  from {
    opacity: 0;
    transform: translateY(-20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 20px;
  border-bottom: 1px solid #eaeaea;
}

.modal-header h3 {
  margin: 0;
  color: #333;
  font-size: 18px;
}

.close-button {
  background: none;
  border: none;
  font-size: 16px;
  color: #666;
  cursor: pointer;
}

.modal-body {
  padding: 20px;
}

.form-group {
  margin-bottom: 15px;
}

.form-group label {
  display: block;
  margin-bottom: 5px;
  font-weight: 500;
  color: #555;
}

.form-group input[type="text"],
.form-group textarea {
  width: 100%;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
  transition: border-color 0.2s;
}

.form-group input[type="text"]:focus,
.form-group textarea:focus {
  border-color: #4776E6;
  outline: none;
}

.form-group textarea {
  height: 100px;
  resize: vertical;
}

.form-group.checkbox {
  display: flex;
  align-items: center;
}

.form-group.checkbox input {
  margin-right: 8px;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 20px;
}

.cancel-button,
.submit-button {
  padding: 8px 16px;
  border: none;
  border-radius: 4px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s;
}

.cancel-button {
  background-color: #f0f0f0;
  color: #333;
}

.submit-button {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
}

.cancel-button:hover {
  background-color: #e0e0e0;
}

.submit-button:hover {
  box-shadow: 0 5px 15px rgba(71, 118, 230, 0.3);
}

.submit-button:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}
</style> 