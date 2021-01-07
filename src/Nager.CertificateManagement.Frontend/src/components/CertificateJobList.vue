<template>
  <q-table
    title="Certificate Jobs"
    dense
    :data="certificateJobs"
    :columns="columns"
    :pagination="pagination"
    row-key="id"
  >
    <template v-slot:body-cell-actions="props">
      <q-td :props="props">
        <q-btn
          v-if="isReadyForDownload(props.row.status)"
          dense
          flat
          color="grey"
          icon="cloud_download"
          @click="download(props.row.id)"
        />
        <q-btn
          dense
          flat
          color="grey"
          icon="delete"
          @click="deleteJob(props.row.id)"
        />
      </q-td>
    </template>
  </q-table>
</template>

<script>
import { date } from 'quasar'

export default {
  name: 'CertificateJobList',
  data () {
    return {
      pagination: {
        page: 1,
        rowsPerPage: 15
      },
      columns: [
        { name: 'fqdn', align: 'left', label: 'FQDN', field: 'fqdn' },
        { name: 'jobType', align: 'left', label: 'Job Type', field: 'jobType' },
        { name: 'created', align: 'left', label: 'Created', field: 'created', format: (val, row) => this.formatDate(val) },
        { name: 'updated', align: 'left', label: 'Updated', field: 'updated', format: (val, row) => this.formatDate(val) },
        { name: 'status', align: 'left', label: 'Status', field: 'status' },
        { name: 'actions', align: 'left', label: 'Actions' }
      ],
      certificateJobs: [],
      autoRefresh: null
    }
  },
  async created () {
    await this.getAll()
    this.autoRefresh = setInterval(this.getAll, 2000)
  },
  beforeDestroy () {
    clearInterval(this.autoRefresh)
  },
  methods: {
    formatDate (dateValue) {
      return dateValue ? `${date.formatDate(dateValue, 'YYYY-MM-DD HH:mm')}` : ''
    },
    isReadyForDownload (status) {
      if (status === 'Done') {
        return true
      }

      return false
    },
    async getAll () {
      try {
        var response = await this.$axios.get('/api/CertificateJob')
        this.certificateJobs = response.data
      } catch (error) {
        this.$q.notify({
          type: 'negative',
          message: 'Request failure',
          caption: `${error}`
        })
      }
    },
    async deleteJob (id) {
      try {
        await this.$axios.delete(`/api/CertificateJob/${id}`)
        await this.getAll()
      } catch (error) {
        this.$q.notify({
          type: 'negative',
          message: 'Request failure',
          caption: `${error}`
        })
      }
    },
    download (id) {
      this.downloadUri(`api/certificatejob/download/${id}`)
    },
    downloadUri (uri, name) {
      var link = document.createElement('a')
      // If you don't know the name or want to use
      // the webserver default set name = ''
      link.setAttribute('download', name)
      link.href = uri
      document.body.appendChild(link)
      link.click()
      link.remove()
    }
  }
}
</script>
